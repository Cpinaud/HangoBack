using Hango.Datos.DTO.Cuenta;
using Hango.Datos.EF;
using Hango.Logica;
using Hango.Logica.Cuenta;
using Hango.Logica.Grupos;
using Hango.Logica.MercadoPago;
using Hango.Logica.Planes;
using Hango.Logica.Usuario;
using Hango.Repositorios;
using Hango.Tests;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Xunit;


public class CuentaLogicaTests
{
    private readonly Mock<IGrupoLogica> mockGrupoLogica;
    private readonly Mock<IPlanLogica> mockPlanLogica;
    private readonly Mock<IMercadoPagoLogica> mockMercadoPagoLogica;
    private readonly Mock<IUsuarioLogica> mockUsuarioLogica;
    private readonly TestHangoContext context;
    private readonly CuentaLogica cuentaLogica;

    private TestHangoContext GetSqliteInMemoryDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<HangoContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(x => x.Ignore(RelationalEventId.AmbientTransactionWarning))
            .Options;

        var ctx = new TestHangoContext(options);
        ctx.Database.EnsureCreated();
        return ctx;



    }

    /*
    public CuentaLogicaTests()
    {
        context = GetSqliteInMemoryDb();

        mockGrupoLogica = new Mock<IGrupoLogica>();
        mockPlanLogica = new Mock<IPlanLogica>();
        mockMercadoPagoLogica = new Mock<IMercadoPagoLogica>();
        mockUsuarioLogica = new Mock<IUsuarioLogica>();

        context.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");

        cuentaLogica = new CuentaLogica(context, mockGrupoLogica.Object, mockPlanLogica.Object, mockMercadoPagoLogica.Object, mockUsuarioLogica.Object);
    }

    
    [Fact]
    public async Task CrearCuentaNueva_DeberiaCrearCuentaYMarcarPagoDeCreador()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();

        // Crear usuarios en base de datos
        var creador = new Usuario { IdUsuario = 1, Nombre = "Juan", Email = "juan@mail.com" };
        var usuario2 = new Usuario { IdUsuario = 2, Nombre = "Ana", Email = "ana@mail.com" };
        var usuario3 = new Usuario { IdUsuario = 3, Nombre = "Luis", Email = "luis@mail.com" };
        context.Usuario.AddRange(creador, usuario2, usuario3);

        // Crear plan con ID 10
        var plan = new Planes
        {
            Id = 10,
            PropuestaId = 1,
            DiaSemana = 5,
            Fecha = DateOnly.FromDateTime(DateTime.Today),
            PreferenciaId = 1,
            Hora = new TimeSpan(20, 0, 0),
            Lugar = "Plaza",
            Descripcion = "Plan de prueba",
            Direccion = "Calle Falsa 123",
            Estado = "Activo",
            Presupuesto = "Medio"
        };
        context.Planes.Add(plan);

        await context.SaveChangesAsync();

        // Mock de IPlanLogica para devolver los usuarios del plan
        var mockPlanLogica = new Mock<IPlanLogica>();
        mockPlanLogica
            .Setup(p => p.ObtenerUsuariosIncluidosEnUnPlan(10))
            .ReturnsAsync(new List<int> { 1, 2, 3 });

        // Mock del resto de las dependencias
        var mockGrupoLogica = new Mock<IGrupoLogica>();
        var mockMercadoPago = new Mock<IMercadoPagoLogica>();
        var mockUsuarioLogica = new Mock<IUsuarioLogica>();

        var cuentaLogica = new CuentaLogica(
            context,
            mockGrupoLogica.Object,
            mockPlanLogica.Object,
            mockMercadoPago.Object,
            mockUsuarioLogica.Object
        );

        // Act
        var idCuenta = await cuentaLogica.CrearCuentaNueva(
            idPlan: 10,
            idUsuario: 1,
            descripcion: "Gastos compartidos",
            montoInicial: 150,
            montoTotal: 450
        );

        // Assert
        var cuenta = await context.Cuenta.FindAsync(idCuenta);
        Assert.NotNull(cuenta);
        Assert.Equal(10, cuenta.IdPlan);
        Assert.Equal(1, cuenta.UsuarioCreadorId);
        Assert.Equal("Abierta", cuenta.Estado);
        Assert.Equal("Gastos compartidos", cuenta.Descripcion);
        Assert.Equal(150, cuenta.MontoInicial);

        var participaciones = await context.ParticipacionCuenta
            .Where(p => p.CuentaId == idCuenta)
            .ToListAsync();

        Assert.Equal(3, participaciones.Count);

        var participacionCreador = participaciones.First(p => p.UsuarioId == 1);
        Assert.Equal("Pagado", participacionCreador.Estado);
        Assert.Equal(150, participacionCreador.MontoGastado);

        var otros = participaciones.Where(p => p.UsuarioId != 1).ToList();
        Assert.All(otros, p =>
        {
            Assert.Equal("Pendiente", p.Estado);
            Assert.Null(p.MontoGastado);
        });
    }
    */


}