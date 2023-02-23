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
        /// Берем что аэропорт в точке 0,0 (можно поменять тут в формуле <see cref="VectorHelper.CalculateDistansce(ApiModels.Base.Coords)"/>)
        /// Считаем, что у нас одна полоса
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

            // добавляем сервисы общения с бортом, диспетчером посадки и наземными службами
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


            // запрос регистрации самолета на вышке
            app.MapPost("/towerControl/registerNewPlane", async (HttpContext httpContext, AppDbContext db, ILogger<Program> logger) =>
            {
                var plane = default(PlaneDTO?);
                var xml = default(TowerControlRegisterNewPlane?);

                try
                {
                    xml = await GetRequestObject<TowerControlRegisterNewPlane>(httpContext, nameof(TowerControlRegisterNewPlane));

                    // изменяем данные сущ. самолет в базе
                    if (db.Planes.Any(x => x.FlightNumber == xml.FlightNumber))
                    {
                        plane = db.Planes.First(x => x.FlightNumber == xml.FlightNumber);
                        plane.SetCoordinate(xml.Coords);
                    }
                    // регистрируем новый самолет в базе
                    else
                    {
                        plane = new() { Date = DateTime.Now, FlightNumber = xml.FlightNumber, Status = PlaneStatusType.REGISTERED };
                        plane.SetCoordinate(xml.Coords);

                        db.Planes.Add(plane);
                    }

                    // сохраняем изменения в бд
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error in [/towerControl/registerNewPlane] request controller");
                    return Results.BadRequest();
                }
            }).Accepts<TowerControlRegisterNewPlane>("application/xml");


            // запрос разрешения на посадку (при условии радиус < 50км, высота менее 2.1км и самолет ранее был зарегистрирован)
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

                    // проверяем что кто-то уже приземляется
                    if (db.Planes.Any(x => x.Status == PlaneStatusType.LANDING))
                    {
                        // то отправляем его на круг
                        await board.SendBoardOnCircuit(plane.FlightNumber, new() { Radius = 50, Speed = 900 });
                        plane.Status = PlaneStatusType.ON_CIRCLE;
                        await db.SaveChangesAsync();
                        return Results.BadRequest();
                    }

                    // проверка статуса и расстояний
                    if ((plane.Status != PlaneStatusType.REGISTERED && plane.Status != PlaneStatusType.ON_CIRCLE)
                        || VectorHelper.CalculateDistansce(plane.GetCoordinate()) > 50000
                        || VectorHelper.CalculateHeight(plane.GetCoordinate()) > 2100)
                        return Results.BadRequest();

                    // передать координаты аэропорта
                    await board.AssignBoardFlyTo(plane.FlightNumber, new() { CoordX = 0, CoordY = 0, CoordZ = 0 });
                    // передать вылетающий из круга самолет диспетчеру подхода
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


            // запрос разрешения на взлет (при условии что никто больше не взлетает)
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

                    // проверяем что кто-то взлетает
                    if (db.Planes.Any(x => x.Status == PlaneStatusType.TAKE_OFF))
                        return Results.BadRequest();

                    // проверка статуса (P.S. возможно надо еще проверить реальные координаты борта из запроса)
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


            // запрос сообщения о подлете к точке, назначенной диспетчером
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


            // запрос передачи заходящего на посадку самолета
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

            // прослушка портов
            //app.Urls.Add("http://localhost:3000");
            //app.Urls.Add("http://localhost:4000");
            app.Run();
        }

        /// <summary>
        /// Настройка политики "retry" схемы для <see cref="HttpClient"/>
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
        /// Извлекает обьект XML'ки из запроса
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpContext"></param>
        /// <param name="className"></param>
        /// <param name="rootName"></param>
        /// <returns></returns>
        private static async Task<T> GetRequestObject<T>(HttpContext httpContext, string className, string rootName = "plane")
        {
            // читаем поток данных и десериализуем xml'ку в обьект
            var data = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            // костыль для сваггера так-как он корневой узел называет именем класса
            data = data.Replace(className, rootName);
            return new XmlSerializerHelper<T>().Deserialize(data);
        }
    }
}