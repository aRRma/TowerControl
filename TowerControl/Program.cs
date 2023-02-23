using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using TowerControl.ApiModels.ApproachControlToTowerControl;
using TowerControl.ApiModels.BoardToTowerControl;
using TowerControl.Data;
using TowerControl.Data.Base;
using TowerControl.Data.DTO;
using TowerControl.Helpers;
using TowerControl.Services;

namespace TowerControl
{
    public class Program
    {
        /// <summary>
        /// ����� ��� �������� � ����� 0,0 (����� �������� ��� � ������� <see cref="VectorHelper.CalculateDistansce(ApiModels.Base.Coords)"/>)
        /// �������, ��� � ��� ���� ������
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.ConfigureLogging(_ =>
            {
                _.ClearProviders();
                _.AddConsole();
            });

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<AppDbContext>(_ => _.UseSqlite());
            builder.Services.AddHttpClient("client", _ =>
            {
                _.Timeout = new TimeSpan(0, 0, 5);
                _.DefaultRequestHeaders.Clear();
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy());

            // ��������� ������� ������� � ������, ����������� ������� � ��������� ��������
            builder.Services.AddScoped<BoardCommandService>();
            builder.Services.AddScoped<ApproachControlCommandService>();
            builder.Services.AddScoped<GroundControlCommandService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();


            // ������ ����������� �������� �� �����
            app.MapPost("/towerControl/registerNewPlane", async (HttpContext httpContext, AppDbContext db, ILogger<Program> logger) =>
            {
                var plane = default(PlaneDTO?);
                var xml = default(TowerControlRegisterNewPlane?);

                try
                {
                    xml = await GetRequestObject<TowerControlRegisterNewPlane>(httpContext, nameof(TowerControlRegisterNewPlane));

                    // �������� ������ ���. ������� � ����
                    if (db.Planes.Any(x => x.FlightNumber == xml.FlightNumber))
                    {
                        plane = db.Planes.First(x => x.FlightNumber == xml.FlightNumber);
                        plane.SetCoordinate(xml.Coords);
                    }
                    // ������������ ����� ������� � ����
                    else
                    {
                        plane = new() { Date = DateTime.Now, FlightNumber = xml.FlightNumber, Status = PlaneStatusType.REGISTERED };
                        plane.SetCoordinate(xml.Coords);

                        db.Planes.Add(plane);
                    }

                    // ��������� ��������� � ��
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error in [/towerControl/registerNewPlane] request controller");
                    return Results.BadRequest();
                }
            }).Accepts<TowerControlRegisterNewPlane>("application/xml");


            // ������ ���������� �� ������� (��� ������� ������ < 50��, ������ ����� 2.1�� � ������� ����� ��� ���������������)
            app.MapPost("/towerControl/landing", async (HttpContext httpContext, AppDbContext db, BoardCommandService board, ApproachControlCommandService approach, ILogger<Program> logger) =>
            {
                var plane = default(PlaneDTO?);
                var xml = default(TowerControlLanding?);

                try
                {
                    xml = await GetRequestObject<TowerControlLanding>(httpContext, nameof(TowerControlLanding));

                    if (!db.Planes.Any(x => x.FlightNumber == xml.FlightNumber))
                        return Results.NotFound();

                    plane = db.Planes.First(x => x.FlightNumber == xml.FlightNumber);
                    plane.SetCoordinate(xml.Coords);
                    plane.LastSpeed = xml.Speed;

                    await db.SaveChangesAsync();

                    // ��������� ��� ���-�� ��� ������������
                    if (db.Planes.Any(x => x.Status == PlaneStatusType.LANDING))
                    {
                        // �� ���������� ��� �� ����
                        await board.SendBoardOnCircuit(plane.FlightNumber, new() { Radius = 50, Speed = 900 });
                        plane.Status = PlaneStatusType.ON_CIRCLE;
                        await db.SaveChangesAsync();
                        return Results.BadRequest();
                    }

                    // �������� ������� � ����������
                    if ((plane.Status != PlaneStatusType.REGISTERED && plane.Status != PlaneStatusType.ON_CIRCLE)
                        || VectorHelper.CalculateDistansce(plane.GetCoordinate()) > 50000
                        || VectorHelper.CalculateHeight(plane.GetCoordinate()) > 2100)
                        return Results.BadRequest();

                    // �������� ���������� ���������
                    await board.AssignBoardFlyTo(plane.FlightNumber, new() { CoordX = 0, CoordY = 0, CoordZ = 0 });
                    // �������� ���������� �� ����� ������� ���������� �������
                    await approach.AddPlane(new() { FlightNumber = plane.FlightNumber, Coords = plane.GetCoordinate() });
                    plane.Status = PlaneStatusType.LANDING;
                    await db.SaveChangesAsync();

                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error in [/towerControl/landing] request controller");
                    return Results.BadRequest();
                }
            }).Accepts<TowerControlLanding>("application/xml");


            // ������ ���������� �� ����� (��� ������� ��� ����� ������ �� ��������)
            app.MapPost("/towerControl/leaving", async (HttpContext httpContext, AppDbContext db, ILogger<Program> logger) =>
            {
                var plane = default(PlaneDTO?);
                var xml = default(TowerControlLeaving?);

                try
                {
                    xml = await GetRequestObject<TowerControlLeaving>(httpContext, nameof(TowerControlLeaving));

                    if (!db.Planes.Any(x => x.FlightNumber == xml.FlightNumber))
                        return Results.NotFound();

                    plane = db.Planes.First(x => x.FlightNumber == xml.FlightNumber);
                    plane.SetCoordinate(xml.Coords);

                    await db.SaveChangesAsync();

                    // ��������� ��� ���-�� ��������
                    if (db.Planes.Any(x => x.Status == PlaneStatusType.TAKE_OFF))
                        return Results.BadRequest();

                    // �������� ������� (P.S. �������� ���� ��� ��������� �������� ���������� ����� �� �������)
                    if (plane.Status != PlaneStatusType.NONE && plane.Status != PlaneStatusType.TAXIING_DONE)
                        return Results.BadRequest();

                    plane.Status = PlaneStatusType.TAKE_OFF;
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error in [/towerControl/leaving] request controller");
                    return Results.BadRequest();
                }
            }).Accepts<TowerControlLanding>("application/xml");


            // ������ ��������� � ������� � �����, ����������� �����������
            app.MapPost("/towerControl/notifyOfReachingDestinationPoint", async (HttpContext httpContext, AppDbContext db, GroundControlCommandService ground, ILogger<Program> logger) =>
            {
                var plane = default(PlaneDTO?);
                var xml = default(TowerControlNotifyOfReachingDestinationPoint?);

                try
                {
                    xml = await GetRequestObject<TowerControlNotifyOfReachingDestinationPoint>(httpContext, nameof(TowerControlNotifyOfReachingDestinationPoint));

                    if (!db.Planes.Any(x => x.FlightNumber == xml.FlightNumber))
                        return Results.NotFound();

                    plane = db.Planes.First(x => x.FlightNumber == xml.FlightNumber);

                    await ground.AddPlane(new() { Coords = plane.GetCoordinate(), FlightNumber = plane.FlightNumber });
                    plane.Status = PlaneStatusType.LANDED;
                    await db.SaveChangesAsync();

                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error in [/towerControl/notifyOfReachingDestinationPoint] request controller");
                    return Results.BadRequest();
                }
            }).Accepts<TowerControlLanding>("application/xml");


            // ������ �������� ���������� �� ������� ��������
            app.MapPost("/towerControl/planes/add", async (HttpContext httpContext, AppDbContext db, ILogger<Program> logger) =>
            {
                var plane = default(PlaneDTO?);
                var xml = default(TowerControlPlaneAdd?);

                try
                {
                    xml = await GetRequestObject<TowerControlPlaneAdd>(httpContext, nameof(TowerControlPlaneAdd));

                    if (!db.Planes.Any(x => x.FlightNumber == xml.FlightNumber))
                    {
                        plane = new() { Date = DateTime.Now, FlightNumber = xml.FlightNumber, Status = PlaneStatusType.LANDING };
                        plane.SetCoordinate(xml.Coords);

                        db.Planes.Add(plane);
                    }
                    else
                    {
                        plane = db.Planes.First(x => x.FlightNumber == xml.FlightNumber);
                        plane.SetCoordinate(xml.Coords);
                        plane.Status = PlaneStatusType.LANDING;
                    }

                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error in [/towerControl/planes/add] request controller");
                    return Results.BadRequest();
                }


            }).Accepts<TowerControlLanding>("application/xml");

            // ��������� ������
            //app.Urls.Add("http://localhost:3000");
            //app.Urls.Add("http://localhost:4000");
            app.Run();
        }

        /// <summary>
        /// ��������� �������� "retry" ����� ��� <see cref="HttpClient"/>
        /// </summary>
        /// <returns></returns>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
               .HandleTransientHttpError()
               .WaitAndRetryAsync(5, retry_attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retry_attempt)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(500)));
        }

        /// <summary>
        /// ��������� ������ XML'�� �� �������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpContext"></param>
        /// <param name="className"></param>
        /// <param name="rootName"></param>
        /// <returns></returns>
        private static async Task<T> GetRequestObject<T>(HttpContext httpContext, string className, string rootName = "plane")
        {
            // ������ ����� ������ � ������������� xml'�� � ������
            var data = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            // ������� ��� �������� ���-��� �� �������� ���� �������� ������ ������
            data = data.Replace(className, rootName);
            return new XmlSerializerHelper<T>().Deserialize(data);
        }
    }
}