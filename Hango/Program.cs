using Hango.Datos.EF;
using Hango.Logica.Horario;
using Hango.Logica.Login;
using Hango.Logica.Preferencia;
using Hango.Logica.Registro;
using Hango.Logica.Usuario;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Hango.Logica.Grupos;
using Hango.Logica.Planes;
using Hango.Logica.Zona;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Hango.Logica.Avatar;
using Hango.Logica.Votacion;
using Hango.Logica.Recuerdos;
using Hango.Logica.Eventos;
using Hango.Logica.Cuenta;
using Hango.Repositorios.Grupo;
using Hango.Repositorios.Zona;
using Hango.Hubs;
using Hango.Dispatcher;
using Microsoft.AspNetCore.SignalR;
using Hango.Logica.MercadoPago;
using Hango.Repositorios.Votacion;
using Hango.Repositorios.Avatar;
using Hango.Logica.Utilidades;
using Hango.Repositorios.Evento;
using Hango.Repositorios.Cuenta;
using Hango.Repositorios.Horario;
using Hango.Logica.Presupuesto;
using Hango.Repositorios.Presupuesto;
using Hango.Repositorios.Preferencia;
using Hango.Repositorios.RefreshToken;
using Hango.Repositorios.Usuario;
using Hango.Repositorios.Utilidades;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<HangoContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HangoConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("La cadena de conexi�n no est� definida en la variable de entorno.");

    options.UseSqlServer(connectionString);
});
builder.Services.AddScoped<ICuentaRepositorio, CuentaRepositorio>();
builder.Services.AddScoped<IGrupoRepositorio, GrupoRepositorio>();
builder.Services.AddScoped<IHorarioDisponibleRepositorio, HorarioDisponibleRepositorio>();
builder.Services.AddScoped<IPreferenciaRepositorio, PreferenciaRepositorio>();
builder.Services.AddScoped<IPresupuestoRepositorio, PresupuestoRepositorio>();
builder.Services.AddScoped<IRefreshTokenRepositorio, RefreshTokenRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IZonaRepositorio, ZonaRepositorio>();
builder.Services.AddScoped<IEventoRepositorio, EventoRepositorio>();
builder.Services.AddScoped<IPresupuestoLogica, PresupuestoLogica>();

builder.Services.AddScoped<IGrupoLogica, GrupoLogica>();
builder.Services.AddScoped<IRegistroLogica, RegistroLogica>();
builder.Services.AddScoped<ILoginLogica, LoginLogica>();
builder.Services.AddScoped<IPreferenciaLogica, PreferenciaLogica>();
builder.Services.AddScoped<IHorarioLogica, HorarioLogica>();
builder.Services.AddScoped<IUnionDeTarea, UnionDeTarea>();
builder.Services.AddScoped<IUsuarioLogica, UsuarioLogica>();

builder.Services.AddScoped<IZonasLogica, ZonasLogica>();
builder.Services.AddScoped<IPlanLogica, PlanLogica>();
builder.Services.AddScoped<IAvatarRepositorio, AvatarRepositorio>();
builder.Services.AddScoped<IAvatarLogica, AvatarLogica>();
builder.Services.AddScoped<ICorreoLogica, CorreoLogica>();
builder.Services.AddScoped<IVotacionRepositorio, VotacionRepositorio>();
builder.Services.AddScoped<IVotacionLogica, VotacionLogica>();
builder.Services.AddScoped<IRecuerdoLogica, RecuerdoLogica>();
builder.Services.AddScoped<IEventoLogica, EventoLogica>();
builder.Services.AddScoped<ICuentaLogica, CuentaLogica>();
builder.Services.AddScoped<ISignalRDispatcher, SignalRDispatcher>();
builder.Services.AddScoped<IMercadoPagoLogica, MercadoPagoLogica>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mi API",
        Version = "v1"
    });
    c.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return new[] { controllerName };
    });

    c.OrderActionsBy(apiDesc =>
    {
        var tags = apiDesc.ActionDescriptor.EndpointMetadata
            .OfType<TagsAttribute>()
            .FirstOrDefault()?.Tags?.FirstOrDefault();

        return tags ?? apiDesc.ActionDescriptor.RouteValues["controller"];
    });

    c.DocInclusionPredicate((docName, apiDesc) => true);
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese 'Bearer'"
    });

 
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "ReactPolicy",
        policy =>
        {

            policy.WithOrigins("http://localhost:5173")  // puerto correr react
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (jwtKey == null)
    throw new InvalidOperationException("jwt no est� definido.");
var key = Encoding.ASCII.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,  
        ValidateAudience = false 
    };
});

builder.Services.AddSignalR(); //para notificaciones en tiempo real


var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mi API v1");
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("ReactPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GrupoHub>("/hub/grupos");

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
