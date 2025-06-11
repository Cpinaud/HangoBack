using Hango.Datos.DTO.Grupo;
using Hango.Datos.DTO.Horario;
using Hango.Datos.DTO.Planes;
using Hango.Datos.DTO.Preferencia;
using Hango.Datos.DTO.Zona;
using Hango.Datos.EF;
using Hango.Logica.Grupos;
using Hango.Logica.Planes;
using Hango.Repositorios.Grupo;
using Hango.Repositorios.Zona;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class TestHangoContext : HangoContext
{
    public TestHangoContext(DbContextOptions<HangoContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Grupo>().Property(e => e.CreateAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Grupo>().Property(e => e.UpdateAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Usuario>().Property(e => e.CreateAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Usuario>().Property(e => e.UpdateAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Usuario_Grupo>().Property(e => e.Ingreso).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Presupuesto>().Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Propuesta>().Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Voto_Plan>().Property(e => e.FechaVoto).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}

public class GrupoLogicaTests
{
    private readonly Mock<IGrupoRepositorio> mockGrupoRepositorio;
    private readonly GrupoLogica grupoLogica;

    // Constantes según tu lógica
    private const int valorPrioridadAgregaManual = 1;
    private const int valorPrioridadAuto = 0;
    private const int limitePreferencias = 3;

    private readonly GrupoInsertDTO grupoInsertDTO;
    private readonly int idUsuario = 1;

    private TestHangoContext GetSqliteInMemoryDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<HangoContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(x => x.Ignore(RelationalEventId.AmbientTransactionWarning))
            .Options;

        var context = new TestHangoContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public GrupoLogicaTests()
    {
        mockGrupoRepositorio = new Mock<IGrupoRepositorio>();

        // Si GrupoLogica requiere otras dependencias, simulalas o pásalas null si no se usan en los tests
        grupoLogica = new GrupoLogica(mockGrupoRepositorio.Object, null, null);

        grupoInsertDTO = new GrupoInsertDTO
        {
            Nombre = "Grupo Test",
            Imagen = null,
            Zonas = new List<string> { "Zona1", "Zona2" }
        };
    }

    public class GrupoLogicaFake : GrupoLogica
    {
        public GrupoLogicaFake(IGrupoRepositorio grupoRepositorio, HangoContext context, IZonaRepositorio zonaRepositorio)
            : base(grupoRepositorio, context, zonaRepositorio)
        {
        }

        // Exponer el método privado como público SOLO PARA TESTS
        public async Task EjecutarSetearPreferenciasGrupoAuto(int idGrupo, int idUsuario)
        {
            var metodo = typeof(GrupoLogica)
                .GetMethod("setearPreferenciasGrupoAuto", BindingFlags.NonPublic | BindingFlags.Instance);

            if (metodo is null)
                throw new Exception("No se pudo encontrar el método privado 'setearPreferenciasGrupoAuto'.");

            var tarea = (Task)metodo.Invoke(this, new object[] { idGrupo, idUsuario });

            await tarea;
        }
    }

    private GrupoLogica CrearGrupoLogicaConMocks()
    {
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();

        // Usamos un contexto de prueba en memoria, aunque no se usa en GuardarImagenAsync
        var options = new DbContextOptionsBuilder<HangoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new HangoContext(options);
        
        return new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);
    }


    [Fact]
    public async Task CrearGrupo_DeberiaCrearGrupoCorrectamente()
    {
        var context = GetSqliteInMemoryDb();

        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Fútbol" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = 1, IdPreferencia = 1 });

        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = 1,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeOnly(14, 0).ToTimeSpan(),
            HorarioFin = new TimeOnly(16, 0).ToTimeSpan()
        });

        context.Zonas.Add(new Zonas { Id = 1, Nombre = "Centro" });
        context.Avatar.Add(new Avatar { IdAvatar = 1, Nombre = "Avatar1", RutaImagen = "ruta.png" });

        await context.SaveChangesAsync();

        var grupoRepo = new GrupoRepositorio(context);
        var zonaRepo = new ZonaRepositorio(context);
        var grupoLogica = new GrupoLogica(grupoRepo, context, zonaRepo);

        var grupoInsert = new GrupoInsertDTO
        {
            Nombre = "Amigos",
            Zonas = new List<string> { "1" } // ID como string
        };

        var grupoCreado = await grupoLogica.CrearGrupo(grupoInsert, 1);

        Assert.NotNull(grupoCreado);
        Assert.Equal("Amigos", grupoCreado.Nombre);
    }

    [Fact]
    public async Task CrearGrupo_DeberiaFallarSiUsuarioTieneElPerfilIncompleto()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();

        // Se necesita un usuario existente por si en alguna validación se usa la FK
        context.Usuario.Add(new Usuario
        {
            IdUsuario = 1,
            Nombre = "Juan",
            Email = "juan@hango.com",
            Password_Hash = "123",
            EstaVerificado = true
        });

        await context.SaveChangesAsync();

        var grupoDto = new GrupoInsertDTO
        {
            Nombre = "Grupo Test",
            Zonas = new List<string> { "1" },
            Imagen = null
        };

        // Mock del repositorio para forzar perfil incompleto
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(r => r.DatosUsuarioSeteados(1)).ReturnsAsync(false);

        var zonaRepoMock = new Mock<IZonaRepositorio>();

        var logica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            logica.CrearGrupo(grupoDto, 1)
        );

        Assert.Equal("No puede crear un grupo, perfil incompleto.", ex.Message);
    }

    [Fact]
    public async Task CrearGrupo_DeberiaFallarSiUsuarioNoTieneDatos()
    {
        var context = GetSqliteInMemoryDb();

        context.Zonas.Add(new Zonas { Id = 1, Nombre = "Centro" });
        context.Avatar.Add(new Avatar { IdAvatar = 1, Nombre = "Avatar1", RutaImagen = "ruta.png" });
        await context.SaveChangesAsync();

        var grupoRepo = new GrupoRepositorio(context);
        var zonaRepo = new ZonaRepositorio(context);
        var grupoLogica = new GrupoLogica(grupoRepo, context, zonaRepo);

        var grupoInsert = new GrupoInsertDTO
        {
            Nombre = "Grupo Sin Usuario",
            Zonas = new List<string> { "1" } // el id tiene que ser valido
        };

        await Assert.ThrowsAsync<Exception>(() => grupoLogica.CrearGrupo(grupoInsert, 999)); // usuario inexistente
    }

    [Fact]
    public async Task CrearGrupo_DeberiaFallarSiNoHayZonasAsignadas()
    {
        var context = GetSqliteInMemoryDb();

        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Fútbol" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = 1, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible { IdUsuario = 1, Fecha = DateTime.Today, DiaSemana = (byte)DateTime.Today.DayOfWeek, HorarioInicio = new TimeSpan(14, 0, 0), HorarioFin = new TimeSpan(16, 0, 0) });

        await context.SaveChangesAsync();

        var mockGrupoRepo = new Mock<IGrupoRepositorio>();
        var mockZonaRepo = new Mock<IZonaRepositorio>();

        var grupoLogica = new GrupoLogica(mockGrupoRepo.Object, context, mockZonaRepo.Object);

        var grupoInsert = new GrupoInsertDTO
        {
            Nombre = "SinZonas",
            Zonas = new List<string>()
        };

        await Assert.ThrowsAsync<Exception>(() => grupoLogica.CrearGrupo(grupoInsert, 1));
    }

    [Fact]
    public async Task ObtenerIntegrantesGrupo_DeberiaDevolverIntegrantesCorrectamente()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();

        // Crear usuarios
        var usuario1 = new Usuario { IdUsuario = 1, Nombre = "Ana", Email = "ana@hango.com", Password_Hash = "123", EstaVerificado = true };
        var usuario2 = new Usuario { IdUsuario = 2, Nombre = "Luis", Email = "luis@hango.com", Password_Hash = "123", EstaVerificado = true };

        // Crear grupo
        var grupo = new Grupo
        {
            Nombre = "Grupo Test",
            TokenInvitacion = "abc123",
            UrlImagen = "img",
            Grupo_Preferencia = new List<Grupo_Preferencia>(),
            Usuario_Grupo = new List<Usuario_Grupo>
        {
            new Usuario_Grupo { IdUsuario = 1, Administrador = true },
            new Usuario_Grupo { IdUsuario = 2, Administrador = false }
        }
        };

        context.Usuario.AddRange(usuario1, usuario2);
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();

        // Forzar navigation properties
        var repo = new GrupoRepositorio(context);
        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var logica = new GrupoLogica(repo, context, zonaRepoMock.Object);

        // Act - el usuario que consulta debe ser parte del grupo
        var integrantes = await logica.ObtenerIntegrantesGrupo(grupo.IdGrupo, usuario1.IdUsuario);

        // Assert
        Assert.Equal(2, integrantes.Count);

        var integrante1 = integrantes.First(i => i.IdUsuario == 1);
        var integrante2 = integrantes.First(i => i.IdUsuario == 2);

        Assert.Equal("Ana", integrante1.Nombre);
        Assert.True(integrante1.Administrador);

        Assert.Equal("Luis", integrante2.Nombre);
        Assert.False(integrante2.Administrador);
    }

    [Fact]
    public async Task ObtenerIntegrantesGrupo_DeberiaLanzarExcepcion_SiRepositorioFalla()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        int idGrupo = 999;
        int idUsuario = 1;

        var grupoRepoMock = new Mock<IGrupoRepositorio>();

        // Asegurar que el usuario sí pertenece, para que llegue al punto donde falla el repo
        grupoRepoMock
            .Setup(r => r.UsuarioPerteneceAlGrupo(idUsuario, idGrupo))
            .ReturnsAsync(true);

        grupoRepoMock
            .Setup(r => r.ObtenerIntegrantesGrupo(idGrupo))
            .ThrowsAsync(new Exception("Fallo interno del repositorio"));

        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var logica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => logica.ObtenerIntegrantesGrupo(idGrupo, idUsuario));

        Assert.Equal("Fallo interno del repositorio", ex.Message);
    }

    [Fact]
    public async Task ObtenerGrupoPorTokenInvitacion_DeberiaRetornarGrupoShortDTO_SiExisteGrupo()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();

        var grupo = new Grupo
        {
            Nombre = "TestGrupo",
            UrlImagen = "test.jpg",
            TokenInvitacion = "token-123"
        };
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();

        var grupoRepo = new GrupoRepositorio(context);
        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var logica = new GrupoLogica(grupoRepo, context, zonaRepoMock.Object);

        // Act
        var resultado = await logica.ObtenerGrupoPorTokenInvitacion("token-123");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(grupo.Nombre, resultado.Nombre);
        Assert.Equal(grupo.UrlImagen, resultado.UrlImagen);
    }

    [Fact]
    public async Task ObtenerGrupoPorTokenInvitacion_DeberiaLanzarExcepcion_SiRepositorioRetornaNull()
    {
        // Arrange
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(r => r.ObtenerGrupoPorToken(It.IsAny<string>()))
            .ReturnsAsync((Grupo)null!); // Forzamos null

        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var context = GetSqliteInMemoryDb();
        var logica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(async () =>
            await logica.ObtenerGrupoPorTokenInvitacion("token-invalido"));
    }

    [Fact]
    public async Task CrearGrupo_DeberiaFallarSiNombreEsVacio()
    {
        var context = GetSqliteInMemoryDb();

        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Fútbol" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = 1, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible { IdUsuario = 1, Fecha = DateTime.Today, DiaSemana = (byte)DateTime.Today.DayOfWeek, HorarioInicio = new TimeSpan(14, 0, 0),
            HorarioFin = new TimeSpan(16, 0, 0)
        });
        context.Zonas.Add(new Zonas { Id = 1, Nombre = "Centro" });

        await context.SaveChangesAsync();

        var mockZonaRepo = new Mock<IZonaRepositorio>();
        var mockGrupoRepo = new Mock<IGrupoRepositorio>();

        var grupoLogica = new GrupoLogica(mockGrupoRepo.Object, context, mockZonaRepo.Object);

        var grupoInsert = new GrupoInsertDTO
        {
            Nombre = "",
            Zonas = new List<string> { "1" }
        };

        await Assert.ThrowsAsync<Exception>(() => grupoLogica.CrearGrupo(grupoInsert, 1));
    }

    [Fact]
    public async Task RegenerarTokenInvitacionGrupo_DeberiaRetornarNuevoToken_SiTodoEsValido()
    {
        // Arrange
        var grupoExistente = new Grupo { IdGrupo = 1, Nombre = "Grupo Test" };

        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(r => r.ObtenerGrupoPorId(1))
                     .ReturnsAsync(grupoExistente);
        grupoRepoMock.Setup(r => r.UsuarioEsAdminDeGrupo(99, 1))
                     .ReturnsAsync(true);
        grupoRepoMock.Setup(r => r.ActualizarToken(It.IsAny<int>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var context = GetSqliteInMemoryDb();
        var logica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act
        var nuevoToken = await logica.RegenerarTokenInvitacionGrupo(1, 99);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(nuevoToken));
        grupoRepoMock.Verify(r => r.ActualizarToken(1, nuevoToken), Times.Once);
    }

    [Fact]
    public async Task RegenerarTokenInvitacionGrupo_DeberiaLanzarExcepcion_SiGrupoNoExiste()
    {
        // Arrange
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(r => r.ObtenerGrupoPorId(It.IsAny<int>()))
                     .ReturnsAsync((Grupo)null!);

        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var context = GetSqliteInMemoryDb();
        var logica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            logica.RegenerarTokenInvitacionGrupo(999, 1));

        Assert.Equal("Grupo no encontrado", ex.Message);
    }

    [Fact]
    public async Task RegenerarTokenInvitacionGrupo_DeberiaLanzarExcepcion_SiNoEsAdmin()
    {
        // Arrange
        var grupoExistente = new Grupo { IdGrupo = 1, Nombre = "Grupo Test" };

        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(r => r.ObtenerGrupoPorId(1))
                     .ReturnsAsync(grupoExistente);
        grupoRepoMock.Setup(r => r.UsuarioEsAdminDeGrupo(5, 1))
                     .ReturnsAsync(false);

        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var context = GetSqliteInMemoryDb();
        var logica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => logica.RegenerarTokenInvitacionGrupo(1, 5));

        Assert.Equal("No tiene permisos para realizar esta acción", ex.Message);
    }

    [Fact]
    public async Task RegenerarTokenInvitacionGrupo_DeberiaLanzarExcepcion_SiFallaActualizacionToken()
    {
        // Arrange
        var grupoExistente = new Grupo { IdGrupo = 1, Nombre = "Grupo Test" };

        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(r => r.ObtenerGrupoPorId(1)).ReturnsAsync(grupoExistente);
        grupoRepoMock.Setup(r => r.UsuarioEsAdminDeGrupo(99, 1)).ReturnsAsync(true);

        // Simula una excepción al intentar actualizar el token
        grupoRepoMock.Setup(r => r.ActualizarToken(1, It.IsAny<string>()))
                     .ThrowsAsync(new Exception("Fallo al guardar token"));

        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var context = GetSqliteInMemoryDb();
        var logica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            logica.RegenerarTokenInvitacionGrupo(1, 99));

        Assert.Equal("Error inesperado al regenerar y guardar el token", ex.Message);
    }

    [Fact]
    public async Task UnirseAlGrupo_DeberiaAgregarIntegranteCorrectamente()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();

        var usuario = new Usuario
        {
            IdUsuario = 1,
            Nombre = "Juan",
            Email = "juan@test.com",
            Password_Hash = "ifhgiudy748e9",
            EstaVerificado = true
        };
        context.Usuario.Add(usuario);
        await context.SaveChangesAsync();

        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Futbol" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = usuario.IdUsuario, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = usuario.IdUsuario,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(9, 0, 0),
            HorarioFin = new TimeSpan(11, 0, 0)
        });
        await context.SaveChangesAsync();

        var grupo = new Grupo
        {
            Nombre = "Asado",
            TokenInvitacion = "gsdsd23",
            UrlImagen = "TUVIEJA"
        };
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();

        var grupoLogica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act
        var resultado = await grupoLogica.UnirseAlGrupo(usuario.IdUsuario, grupo.IdGrupo);

        // Assert
        Assert.True(resultado);
        var relacion = await context.Usuario_Grupo
            .FirstOrDefaultAsync(ug => ug.IdUsuario == usuario.IdUsuario && ug.IdGrupo == grupo.IdGrupo);
        Assert.NotNull(relacion);
        Assert.False(relacion.Administrador);
    }

    [Fact]
    public async Task UnirseAlGrupo_DeberiaFallarSiYaEsIntegrante()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();

        var usuario = new Usuario
        {
            IdUsuario = 1,
            Nombre = "Juan",
            Email = "juan@test.com",
            Password_Hash = "ifhgiudy748e9",
            EstaVerificado = true
        };
        context.Usuario.Add(usuario);
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Rock" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = usuario.IdUsuario, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = usuario.IdUsuario,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(10, 0, 0),
            HorarioFin = new TimeSpan(12, 0, 0)
        });
        await context.SaveChangesAsync();

        var grupo = new Grupo
        {
            Nombre = "Asado",
            TokenInvitacion = "gsdsd23",
            UrlImagen = "TUVIEJA"
        };
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();

        // 2.1 Agregar manualmente la relación
        context.Usuario_Grupo.Add(new Usuario_Grupo
        {
            IdUsuario = usuario.IdUsuario,
            IdGrupo = grupo.IdGrupo,
            Administrador = false,
            Ingreso = DateTime.Now
        });
        await context.SaveChangesAsync();

        var grupoLogica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            grupoLogica.UnirseAlGrupo(usuario.IdUsuario, grupo.IdGrupo));
        Assert.Contains("El usuario ya pertenece al grupo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnirseAlGrupo_DeberiaFallarSiDatosUsuarioIncompletos()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();

        var usuario = new Usuario { Nombre = "Luis", Email = "luis@x.com", Password_Hash = "pwd" };
        // NO agregamos horario ni preferencia
        context.Usuario.Add(usuario);
        await context.SaveChangesAsync();

        var grupo = new Grupo
        {
            Nombre = "Asado",
            TokenInvitacion = "gsdsd23",
            UrlImagen = "TUVIEJA"
        };
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();

        var grupoLogica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            grupoLogica.UnirseAlGrupo(usuario.IdUsuario, grupo.IdGrupo));
        Assert.Contains("No puede unirse al grupo, perfil incompleto.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task ActivarGrupo_DeberiaLanzarExcepcion_SiGrupoNoExiste()
    {
        // Arrange
        var grupoRepo = new Mock<IGrupoRepositorio>();

        grupoRepo
            .Setup(r => r.ObtenerGrupoPorId(1))
            .ReturnsAsync((Grupo)null!); // Fuerza que no exista el grupo

        // No hace falta mockear las demás llamadas porque no se ejecutan si el grupo es null

        var grupoLogica = new GrupoLogica(grupoRepo.Object, new Mock<HangoContext>().Object, new Mock<IZonaRepositorio>().Object
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => grupoLogica.ActivarGrupo(1, 99)); // cualquier idUsuario
        Assert.Equal("Grupo no encontrado", ex.Message);
    }


    [Fact]
    public async Task ActivarGrupo_DeberiaLanzarExcepcion_SiHayMenosDeDosIntegrantes()
    {
        using var context = GetSqliteInMemoryDb();

        // Insertamos un usuario válido
        var usuario = new Usuario { IdUsuario = 1, Nombre = "Luis", Email = "luis@x.com", Password_Hash = "pwd" };
        context.Usuario.Add(usuario);

        // Insertamos un grupo válido
        var grupo = new Grupo
        {
            IdGrupo = 1,
            Nombre = "Truco",
            TokenInvitacion = "abc123",
            UrlImagen = "url.jpg"
        };
        context.Grupo.Add(grupo);

        // Solo 1 integrante
        context.Usuario_Grupo.Add(new Usuario_Grupo { IdUsuario = 1, IdGrupo = 1 });
        await context.SaveChangesAsync();

        var grupoLogica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => grupoLogica.ActivarGrupo(1, 1)); // <-- Segundo parámetro agregado
        Assert.Equal("No se puede activar un grupo con menos de 2 integrantes", ex.Message);
    }


    [Fact]
    public async Task ObtenerPlanesConfirmadosDelGrupo_DeberiaRetornarPlanesDelGrupo()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HangoContext>()
            .UseSqlite("Filename=:memory:")
            .Options;

        await using var context = new TestHangoContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var idGrupo = 1;
        var idUsuario = 99; // Podés usar cualquier ID, mientras lo simules en el mock

        var propuesta = new Propuesta
        {
            IdPropuesta = 101,
            GrupoId = idGrupo,
            FechaCreacion = DateTime.Now,
            FechaVencimiento = DateTime.Now.AddDays(7),
            Origen = "IA"
        };

        var planes = new List<Planes>
    {
        new Planes
        {
            Id = 1,
            PropuestaId = propuesta.IdPropuesta,
            DiaSemana = 3,
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            PreferenciaId = 5,
            Hora = new TimeSpan(20, 0, 0),
            Lugar = "Café Martínez",
            Descripcion = "Merienda grupal con amigos",
            Direccion = "Av. San Martín 123",
            Estado = "Confirmado",
            Presupuesto = "$$",
            Propuesta = propuesta
        }
    };

        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(g => g.UsuarioPerteneceAlGrupo(idUsuario, idGrupo)).ReturnsAsync(true); // Mock necesario
        grupoRepoMock.Setup(g => g.ObtenerPlanesConfirmadosDelGrupo(idGrupo)).ReturnsAsync(planes);

        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act
        var resultado = await grupoLogica.ObtenerPlanesConfirmadosDelGrupo(idGrupo, idUsuario);

        // Assert
        Assert.Single(resultado);
        var plan = resultado.First();
        Assert.Equal("Café Martínez", plan.Lugar);
        Assert.Equal("Merienda grupal con amigos", plan.Descripcion);
        Assert.Equal("Confirmado", plan.Estado);
        Assert.Equal("$$", plan.Presupuesto);
        Assert.Equal(idGrupo, plan.Propuesta.GrupoId);
    }

    [Fact]
    public async Task ObtenerPlanesConfirmadosDelGrupo_DeberiaLanzarExcepcion_SiUsuarioNoPerteneceAlGrupo()
    {
        // Arrange
        var idGrupo = 1;
        var idUsuario = 42;

        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(r => r.UsuarioPerteneceAlGrupo(idUsuario, idGrupo)).ReturnsAsync(false);

        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, new Mock<HangoContext>().Object, new Mock<IZonaRepositorio>().Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => grupoLogica.ObtenerPlanesConfirmadosDelGrupo(idGrupo, idUsuario));
        Assert.Equal("El usuario no pertenece al grupo", ex.Message);
    }

    [Fact]
    public async Task ActivarGrupo_DeberiaActivarseCorrectamente()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();

        var grupo = new Grupo
        {
            IdGrupo = 1,
            Nombre = "Asado",
            TokenInvitacion = "gsdsd23",
            UrlImagen = "TUVIEJA"
        };

        var integrantes = new List<Usuario_Grupo>
    {
        new Usuario_Grupo { IdUsuario = 1, IdGrupo = 1 },
        new Usuario_Grupo { IdUsuario = 2, IdGrupo = 1 }
    };

        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        grupoRepoMock.Setup(r => r.ObtenerGrupoPorId(1)).ReturnsAsync(grupo);
        grupoRepoMock.Setup(r => r.ObtenerIntegrantesGrupo(1)).ReturnsAsync(integrantes);
        grupoRepoMock.Setup(r => r.UsuarioPerteneceAlGrupo(1, 1)).ReturnsAsync(true); // <- Esto también es necesario
        grupoRepoMock.Setup(r => r.ActivarGrupo(1)).Returns(Task.CompletedTask).Verifiable();

        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, new ZonaRepositorio(context));

        // Act
        await grupoLogica.ActivarGrupo(1, 1); // <-- Se pasa también el idUsuario

        // Assert
        grupoRepoMock.Verify(r => r.ActivarGrupo(1), Times.Once);
    }

    [Fact]
    public async Task EliminarIntegranteGrupo_DeberiaEliminarCorrectamenteSiEsAdmin()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();

        var admin = new Usuario { Nombre = "Admin", Email = "admin@x.com", Password_Hash = "pwd" };
        var miembro = new Usuario { Nombre = "Miembro", Email = "miembro@x.com", Password_Hash = "pwd" };
        // Llenar datos de ambos para que Unirse/CrearGrupo permita
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Jazz" });
        context.Usuario.AddRange(admin, miembro);
        await context.SaveChangesAsync();
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = admin.IdUsuario, IdPreferencia = 1 });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = miembro.IdUsuario, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = admin.IdUsuario,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(8, 0, 0),
            HorarioFin = new TimeSpan(10, 0, 0)
        });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = miembro.IdUsuario,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(8, 0, 0),
            HorarioFin = new TimeSpan(10, 0, 0)
        });
        await context.SaveChangesAsync();

        // Creamos el grupo y agregamos ambos: admin como Admin, miembro como no-admin
        var grupo = new Grupo
        {
            Nombre = "Asado",
            TokenInvitacion = "gsdsd23",
            UrlImagen = "TUVIEJA"
        };
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();
        context.Usuario_Grupo.Add(new Usuario_Grupo
        {
            IdUsuario = admin.IdUsuario,
            IdGrupo = grupo.IdGrupo,
            Administrador = true,
            Ingreso = DateTime.Now
        });
        context.Usuario_Grupo.Add(new Usuario_Grupo
        {
            IdUsuario = miembro.IdUsuario,
            IdGrupo = grupo.IdGrupo,
            Administrador = false,
            Ingreso = DateTime.Now
        });
        await context.SaveChangesAsync();

        var grupoLogica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act
        await grupoLogica.EliminarIntegranteGrupo(grupo.IdGrupo, miembro.IdUsuario, admin.IdUsuario);

        // Assert
        var sigueMiembro = await context.Usuario_Grupo
            .AnyAsync(ug => ug.IdGrupo == grupo.IdGrupo && ug.IdUsuario == miembro.IdUsuario);
        Assert.False(sigueMiembro);
    }

    [Fact]
    public async Task EliminarIntegranteGrupo_DeberiaFallarSiNoEsIntegrante()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();

        var admin = new Usuario { Nombre = "Admin2", Email = "admin2@x.com", Password_Hash = "pwd" };
        var ajeno = new Usuario { Nombre = "Ajeno", Email = "ajeno@x.com", Password_Hash = "pwd" };
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Pop" });
        context.Usuario.AddRange(admin, ajeno);
        await context.SaveChangesAsync();
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = admin.IdUsuario, IdPreferencia = 1 });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = ajeno.IdUsuario, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = admin.IdUsuario,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(7, 0, 0),
            HorarioFin = new TimeSpan(9, 0, 0)
        });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = ajeno.IdUsuario,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(7, 0, 0),
            HorarioFin = new TimeSpan(9, 0, 0)
        });
        await context.SaveChangesAsync();

        var grupo = new Grupo
        {
            Nombre = "Asado",
            TokenInvitacion = "gsdsd23",
            UrlImagen = "TUVIEJA"
        };
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();
        // Solo agregamos al admin
        context.Usuario_Grupo.Add(new Usuario_Grupo
        {
            IdUsuario = admin.IdUsuario,
            IdGrupo = grupo.IdGrupo,
            Administrador = true,
            Ingreso = DateTime.Now
        });
        await context.SaveChangesAsync();

        var grupoLogica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            grupoLogica.EliminarIntegranteGrupo(grupo.IdGrupo, ajeno.IdUsuario, admin.IdUsuario));
        Assert.Contains("no pertenece", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearGrupo_DeberiaCrearConImagenNulaSinExcepcion()
    {
        var options = new DbContextOptionsBuilder<HangoContext>()
            .UseSqlite("Filename=:memory:")
            .Options;

        using var context = new HangoContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        var mockGrupoRepo = new Mock<IGrupoRepositorio>();
        var mockZonaRepo = new Mock<IZonaRepositorio>();

        var grupoInsertDto = new GrupoInsertDTO
        {
            Nombre = "Grupo Test",
            Zonas = new List<string> { "1" },
            Imagen = null
        };
        int usuarioId = 1;

        mockGrupoRepo.Setup(r => r.DatosUsuarioSeteados(usuarioId)).ReturnsAsync(true);
        mockGrupoRepo.Setup(r => r.GuardarGrupo(It.IsAny<Grupo>()))
            .Callback<Grupo>(g => g.IdGrupo = 123)
            .Returns(Task.CompletedTask);

        mockZonaRepo.Setup(z => z.ObtenerZonaPorId(It.IsAny<int>()))
            .ReturnsAsync(new Zonas { Id = 1, Nombre = "Zona 1" });

        mockZonaRepo.Setup(z => z.GuardarZonaEnGrupo(It.IsAny<Zonas_Grupos>()))
            .Returns(Task.CompletedTask);

        mockGrupoRepo.Setup(r => r.GuardarUsuarioGrupo(It.IsAny<Usuario_Grupo>()))
            .Returns(Task.CompletedTask);

        // SETUPS NECESARIOS PARA EL MÉTODO setearPreferenciasGrupoAuto
        mockGrupoRepo.Setup(r => r.ObtenerIdPreferenciasGrupoPorTipo(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<int>());

        mockGrupoRepo.Setup(r => r.ObtenerIntegrantesGrupo(It.IsAny<int>()))
            .ReturnsAsync(new List<Usuario_Grupo>());

        mockGrupoRepo.Setup(r => r.ObtenerIdPreferenciasComunesOrdenadas(It.IsAny<int>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new List<int>());

        mockGrupoRepo.Setup(r => r.ObtenerPreferenciasGrupoPorTipo(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Grupo_Preferencia>());

        mockGrupoRepo.Setup(r => r.EliminarPreferenciasGrupoAuto(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        mockGrupoRepo.Setup(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()))
            .Returns(Task.CompletedTask);

        var grupoLogica = new GrupoLogica(mockGrupoRepo.Object, context, mockZonaRepo.Object);

        // Act
        var resultado = await grupoLogica.CrearGrupo(grupoInsertDto, usuarioId);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Grupo Test", resultado.Nombre);
        Assert.Equal(123, resultado.IdGrupo);
        Assert.Equal("/avatar-group/default.png", resultado.UrlImagen);
    }


    [Fact]
    public async Task ObtenerGruposUsuario_DeberiaRetornarGruposDelUsuario()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();
        var usuario = new Usuario { IdUsuario = 1, Nombre = "Juan", Email = "juan@test.com", Password_Hash = "ifhgiudy748e9", EstaVerificado = true };
        var grupo1 = new Grupo { Nombre = "Asado", TokenInvitacion = "gsdsd23", UrlImagen = "TUVIEJA", Usuario_Grupo = new List<Usuario_Grupo> { new Usuario_Grupo { IdUsuario = 1 } } };
        var grupo2 = new Grupo { Nombre = "Fútbol", TokenInvitacion = "gsdsd23", UrlImagen = "TUVIEJA", Usuario_Grupo = new List<Usuario_Grupo> { new Usuario_Grupo { IdUsuario = 1 } } };

        context.Usuario.Add(usuario);
        context.Grupo.AddRange(grupo1, grupo2);
        await context.SaveChangesAsync();

        var logica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act
        var resultado = await logica.ObtenerGruposDelUsuario(1);

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, g => g.Nombre == "Asado");
        Assert.Contains(resultado, g => g.Nombre == "Fútbol");
    }

    [Fact]
    public async Task ObtenerGruposUsuario_DeberiaRetornarListaVaciaSiUsuarioNoTieneGrupos()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();
        var usuario = new Usuario { IdUsuario = 1, Nombre = "Juan", Email = "juan@test.com", Password_Hash = "ifhgiudy748e9", EstaVerificado = true };
        context.Usuario.Add(usuario);
        await context.SaveChangesAsync();

        var logica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act
        var resultado = await logica.ObtenerGruposDelUsuario(1);

        // Assert
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObtenerGruposUsuario_DeberiaRetornarListaVaciaSiUsuarioNoExiste()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();
        var logica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act
        var resultado = await logica.ObtenerGruposDelUsuario(999); // Usuario inexistente

        // Assert
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task CrearGrupo_DeberiaAsignarZonasCorrectamente()
    {
        var context = GetSqliteInMemoryDb();

        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Fútbol" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = 1, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible { IdUsuario = 1, Fecha = DateTime.Today, DiaSemana = (byte)DateTime.Today.DayOfWeek, HorarioInicio = new TimeSpan(14, 0, 0), HorarioFin = new TimeSpan(16, 0, 0) });
        context.Zonas.Add(new Zonas { Id = 1, Nombre = "Centro" });

        await context.SaveChangesAsync();

        var grupoRepo = new GrupoRepositorio(context);
        var zonaRepo = new ZonaRepositorio(context);
        var grupoLogica = new GrupoLogica(grupoRepo, context, zonaRepo);

        var grupoInsert = new GrupoInsertDTO
        {
            Nombre = "ConZonas",
            Zonas = new List<string> { "1" }
        };

        var resultado = await grupoLogica.CrearGrupo(grupoInsert, 1);

        var zonasGrupo = context.Zonas_Grupos.Where(zg => zg.IdGrupo == resultado.IdGrupo).ToList();

        Assert.Single(zonasGrupo);
        Assert.Equal(1, zonasGrupo[0].IdZona);
    }

    [Fact]
    public async Task CrearGrupo_DeberiaAsignarPreferenciasAutomaticas()
    {
        var context = GetSqliteInMemoryDb();

        var usuario = new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" };
        var preferencia = new Preferencia { IdPreferencia = 1, Nombre = "Fútbol" };

        context.Usuario.Add(usuario);
        context.Preferencia.Add(preferencia);
        await context.SaveChangesAsync();

        var mockGrupoRepo = new Mock<IGrupoRepositorio>();
        var mockZonaRepo = new Mock<IZonaRepositorio>();

        var grupoInsert = new GrupoInsertDTO
        {
            Nombre = "Grupo Preferencias",
            Zonas = new List<string> { "1" }
        };

        int grupoCreadoId = 99;

        mockGrupoRepo.Setup(r => r.DatosUsuarioSeteados(usuario.IdUsuario))
            .ReturnsAsync(true);

        mockGrupoRepo.Setup(r => r.GuardarGrupo(It.IsAny<Grupo>()))
            .Callback<Grupo>(g => g.IdGrupo = grupoCreadoId)
            .Returns(Task.CompletedTask);

        mockGrupoRepo.Setup(r => r.ObtenerIntegrantesGrupo(grupoCreadoId))
            .ReturnsAsync(new List<Usuario_Grupo> { new Usuario_Grupo { IdUsuario = usuario.IdUsuario } });

        mockGrupoRepo.Setup(r => r.ObtenerIdPreferenciasGrupoPorTipo(grupoCreadoId, It.IsAny<int>()))
            .ReturnsAsync(new List<int>()); // <- Este era el que faltaba

        mockGrupoRepo.Setup(r => r.ObtenerIdPreferenciasComunesOrdenadas(grupoCreadoId, It.IsAny<List<int>>()))
            .ReturnsAsync(new List<int> { preferencia.IdPreferencia });

        mockGrupoRepo.Setup(r => r.ObtenerPreferenciasGrupoPorTipo(grupoCreadoId, 0)) // 0 = valorPrioridadAuto
            .ReturnsAsync(new List<Grupo_Preferencia>());

        mockGrupoRepo.Setup(r => r.EliminarPreferenciasGrupoAuto(grupoCreadoId))
            .Returns(Task.CompletedTask);

        mockGrupoRepo.Setup(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()))
            .Returns(Task.CompletedTask);

        mockGrupoRepo.Setup(r => r.GuardarUsuarioGrupo(It.IsAny<Usuario_Grupo>()))
            .Returns(Task.CompletedTask);

        mockZonaRepo.Setup(z => z.ObtenerZonaPorId(1))
            .ReturnsAsync(new Zonas { Id = 1, Nombre = "Zona 1" });

        mockZonaRepo.Setup(z => z.GuardarZonaEnGrupo(It.IsAny<Zonas_Grupos>()))
            .Returns(Task.CompletedTask);

        var grupoLogica = new GrupoLogica(mockGrupoRepo.Object, context, mockZonaRepo.Object);

        var resultado = await grupoLogica.CrearGrupo(grupoInsert, usuario.IdUsuario);

        Assert.NotNull(resultado);
        Assert.Equal("Grupo Preferencias", resultado.Nombre);
        Assert.Equal(grupoCreadoId, resultado.IdGrupo);
    }

    [Fact]
    public async Task CrearGrupo_DeberiaFallarSiIdZonaNoEsNumerico()
    {
        var context = GetSqliteInMemoryDb();

        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Fútbol" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = 1, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = 1,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(14, 0, 0),
            HorarioFin = new TimeSpan(16, 0, 0)
        });

        await context.SaveChangesAsync();

        var grupoRepo = new GrupoRepositorio(context);
        var zonaRepo = new ZonaRepositorio(context);
        var grupoLogica = new GrupoLogica(grupoRepo, context, zonaRepo);

        var grupoInsert = new GrupoInsertDTO
        {
            Nombre = "GrupoZonasInvalidas",
            Zonas = new List<string> { "invalid" }
        };

        await Assert.ThrowsAsync<FormatException>(() => grupoLogica.CrearGrupo(grupoInsert, 1));
    }


    [Fact]
    public async Task ActualizarGrupo_DeberiaActualizarNombreCorrectamente()
    {
        var context = GetSqliteInMemoryDb();

        // Crear el usuario necesario para respetar FK
        var usuario = new Usuario
        {
            IdUsuario = 1,
            Nombre = "Juan",
            Email = "juan@hango.com",
            Password_Hash = "123",
            EstaVerificado = true
        };
        context.Usuario.Add(usuario);

        // Crear y guardar el grupo con el usuario como integrante
        var grupo = new Grupo
        {
            IdGrupo = 1,
            Nombre = "LosSkynet",
            TokenInvitacion = "gsdsd23",
            UrlImagen = "TUVIEJA",
            Usuario_Grupo = new List<Usuario_Grupo>
        {
            new Usuario_Grupo { IdUsuario = 1, IdGrupo = 1 }
        }
        };
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();

        // Instanciar lógica
        var repo = new GrupoRepositorio(context);
        var zonaRepo = new Mock<IZonaRepositorio>();
        var logica = new GrupoLogica(repo, context, zonaRepo.Object);

        // DTO de actualización
        var dto = new GrupoUpdateDTO
        {
            Nombre = "NuevoNombre",
            ZonasJson = JsonConvert.SerializeObject(new List<ZonaDTO>()),
            PreferenciasJson = JsonConvert.SerializeObject(new List<PreferenciaDTO>())
        };

        // Act
        var resultado = await logica.ActualizarGrupo(1, dto, 1); // nuevo parámetro: idUsuario

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("NuevoNombre", resultado.Nombre);
    }

    [Fact]
    public async Task ActualizarGrupo_DeberiaLanzarExcepcionSiExcedeElLimiteDePreferencias()
    {
        var context = GetSqliteInMemoryDb();

        context.Usuario.Add(new Usuario
        {
            IdUsuario = 1,
            Nombre = "Juan",
            Email = "juan@hango.com",
            Password_Hash = "123",
            EstaVerificado = true
        });

        context.Grupo.Add(new Grupo
        {
            Nombre = "GrupoA",
            TokenInvitacion = "abc123",
            UrlImagen = "url",
            Usuario_Grupo = new List<Usuario_Grupo>
        {
            new Usuario_Grupo { IdUsuario = 1 }
        }
        });

        await context.SaveChangesAsync();

        var repo = new GrupoRepositorio(context);
        var zonaRepo = new Mock<IZonaRepositorio>();
        var logica = new GrupoLogica(repo, context, zonaRepo.Object);

        var muchasPreferencias = Enumerable.Range(1, 6).Select(i => new PreferenciaDTO { IdPreferencia = i }).ToList();

        var dto = new GrupoUpdateDTO
        {
            PreferenciasJson = JsonConvert.SerializeObject(muchasPreferencias),
            ZonasJson = JsonConvert.SerializeObject(new List<ZonaDTO>())
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => logica.ActualizarGrupo(1, dto, 1));
        Assert.Equal("No se pueden agregar más de 5 preferencias al grupo.", ex.Message);
    } 

    
    [Fact]
    public async Task ActualizarGrupo_DeberiaLanzarExcepcion_SiNombreMuyLargo()
    {
        var context = GetSqliteInMemoryDb();

        context.Usuario.Add(new Usuario
        {
            IdUsuario = 1,
            Nombre = "Juan",
            Email = "juan@hango.com",
            Password_Hash = "123",
            EstaVerificado = true
        });

        context.Grupo.Add(new Grupo
        {
            Nombre = "GrupoB",
            TokenInvitacion = "xyz789",
            UrlImagen = "url",
            Usuario_Grupo = new List<Usuario_Grupo>
        {
            new Usuario_Grupo { IdUsuario = 1 }
        }
        });

        await context.SaveChangesAsync();

        var repo = new GrupoRepositorio(context);
        var zonaRepo = new Mock<IZonaRepositorio>();
        var logica = new GrupoLogica(repo, context, zonaRepo.Object);

        var nombreLargo = new string('A', 151);

        var dto = new GrupoUpdateDTO
        {
            Nombre = nombreLargo,
            ZonasJson = JsonConvert.SerializeObject(new List<ZonaDTO>()),
            PreferenciasJson = JsonConvert.SerializeObject(new List<PreferenciaDTO>())
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => logica.ActualizarGrupo(1, dto, 1));
        Assert.Equal("El nombre ingresado no es válido", ex.Message);
    }


    [Fact]
    public async Task ActualizarGrupo_DeberiaLanzarExcepcion_SiGrupoNoExiste()
    {
        var context = GetSqliteInMemoryDb();

        var mockGrupoRepo = new Mock<IGrupoRepositorio>();
        mockGrupoRepo.Setup(r => r.UsuarioPerteneceAlGrupo(It.IsAny<int>(), It.IsAny<int>()))
                     .ReturnsAsync(true); // Simula que pertenece
        mockGrupoRepo.Setup(r => r.ObtenerGrupoPorId(It.IsAny<int>()))
                     .ReturnsAsync((Grupo)null!); // Simula que el grupo no existe

        var zonaRepo = new Mock<IZonaRepositorio>();
        var logica = new GrupoLogica(mockGrupoRepo.Object, context, zonaRepo.Object);

        var dto = new GrupoUpdateDTO
        {
            Nombre = "GrupoInexistente",
            ZonasJson = JsonConvert.SerializeObject(new List<ZonaDTO>()),
            PreferenciasJson = JsonConvert.SerializeObject(new List<PreferenciaDTO>())
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => logica.ActualizarGrupo(99, dto, 1));
        Assert.Equal("El grupo no existe", ex.Message);
    }

    [Fact]
    public async Task EliminarIntegranteGrupo_DeberiaFallarSiNoEsAdmin()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();

        var admin = new Usuario { Nombre = "Admin3", Email = "admin3@x.com", Password_Hash = "pwd" };
        var miembro = new Usuario { Nombre = "Miembro2", Email = "miembro2@x.com", Password_Hash = "pwd" };
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Blues" });
        context.Usuario.AddRange(admin, miembro);
        await context.SaveChangesAsync();
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = admin.IdUsuario, IdPreferencia = 1 });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = miembro.IdUsuario, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = admin.IdUsuario,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(6, 0, 0),
            HorarioFin = new TimeSpan(8, 0, 0)
        });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = miembro.IdUsuario,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(6, 0, 0),
            HorarioFin = new TimeSpan(8, 0, 0)
        });
        await context.SaveChangesAsync();

        var grupo = new Grupo
        {
            Nombre = "Asado",
            TokenInvitacion = "gsdsd23",
            UrlImagen = "TUVIEJA"
        };
        context.Grupo.Add(grupo);
        await context.SaveChangesAsync();

        // Agregamos ambos, pero solo el admin es admin
        context.Usuario_Grupo.Add(new Usuario_Grupo
        {
            IdUsuario = admin.IdUsuario,
            IdGrupo = grupo.IdGrupo,
            Administrador = true,
            Ingreso = DateTime.Now
        });
        context.Usuario_Grupo.Add(new Usuario_Grupo
        {
            IdUsuario = miembro.IdUsuario,
            IdGrupo = grupo.IdGrupo,
            Administrador = false,
            Ingreso = DateTime.Now
        });
        await context.SaveChangesAsync();

        var grupoLogica = new GrupoLogica(new GrupoRepositorio(context), context, new ZonaRepositorio(context));

        // Act & Assert: "miembro" intenta eliminar a "admin" → no es admin, debería fallar
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            grupoLogica.EliminarIntegranteGrupo(grupo.IdGrupo, admin.IdUsuario, miembro.IdUsuario));
        Assert.Contains("No tiene permisos para realizar esta acción", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EliminarIntegranteGrupo_DeberiaFallar_SiUsuarioNoPerteneceAlGrupo()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();
        var grupoRepo = new Mock<IGrupoRepositorio>();
        grupoRepo.Setup(r => r.UsuarioPerteneceAlGrupo(2, 1)).ReturnsAsync(false);

        var logica = new GrupoLogica(grupoRepo.Object, context, Mock.Of<IZonaRepositorio>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => logica.EliminarIntegranteGrupo(1, 2, 3));
        Assert.Equal("El usuario que intenta eliminar no pertenece al grupo", ex.Message);
    }

    [Fact]
    public async Task EliminarIntegranteGrupo_DeberiaFallar_SiEsAdminYUnicoIntegrante()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();
        var grupoRepo = new Mock<IGrupoRepositorio>();
        grupoRepo.Setup(r => r.UsuarioPerteneceAlGrupo(2, 1)).ReturnsAsync(true);
        grupoRepo.Setup(r => r.UsuarioEsAdminDeGrupo(2, 1)).ReturnsAsync(true);
        grupoRepo.Setup(r => r.GrupoConUnicoIntegrante(1)).ReturnsAsync(true);
        grupoRepo.Setup(r => r.UsuarioEsAdminDeGrupo(3, 1)).ReturnsAsync(true);
        grupoRepo.Setup(r => r.ObtenerIntegranteDeUnGrupoPorId(1, 2)).ReturnsAsync(new Usuario_Grupo());

        var logica = new GrupoLogica(grupoRepo.Object, context, Mock.Of<IZonaRepositorio>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => logica.EliminarIntegranteGrupo(1, 2, 3));
        Assert.Equal("No puede abandonar siendo el único integrante. Se debe eliminar el grupo", ex.Message);
    }
    [Fact]
    public async Task CrearGrupo_DeberiaCrearConZonasValidas()
    {
        using var context = GetSqliteInMemoryDb();

        // Arrange
        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Fútbol" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = 1, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = 1,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(14, 0, 0),
            HorarioFin = new TimeSpan(16, 0, 0)
        });

        var zona = new Zonas { Id = 1, Nombre = "Palermo" };
        context.Zonas.Add(zona);
        await context.SaveChangesAsync();

        // Forzamos que el usuario tenga datos seteados
        var grupoRepo = new GrupoRepositorio(context);
        var zonaRepo = new ZonaRepositorio(context);

        var grupoDTO = new GrupoInsertDTO
        {
            Nombre = "Juntada",
            Zonas = new List<string> { "1" }
        };

        var logica = new GrupoLogica(grupoRepo, context, zonaRepo);

        // Act
        var grupoCreado = await logica.CrearGrupo(grupoDTO, 1);

        // Assert
        var zonasGrupo = await context.Zonas_Grupos.Where(zg => zg.IdGrupo == grupoCreado.IdGrupo).ToListAsync();
        Assert.Single(zonasGrupo);
        Assert.Equal(1, zonasGrupo.First().IdZona);
    }

    [Fact]
    public async Task CrearGrupo_DeberiaFallarSiZonaNoExiste()
    {
        var context = GetSqliteInMemoryDb();

        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Fútbol" });
        context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = 1, IdPreferencia = 1 });
        context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = 1,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(14, 0, 0),
            HorarioFin = new TimeSpan(16, 0, 0)
        });

        await context.SaveChangesAsync();

        var grupoRepo = new GrupoRepositorio(context);
        var zonaRepo = new ZonaRepositorio(context);
        var grupoLogica = new GrupoLogica(grupoRepo, context, zonaRepo);

        var grupoInsert = new GrupoInsertDTO
        {
            Nombre = "GrupoZonasInexistentes",
            Zonas = new List<string> { "999" }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => grupoLogica.CrearGrupo(grupoInsert, 1));
        Assert.Equal("No se pudo encontrar la zona", ex.Message);
    }


    [Fact]
    public async Task CrearGrupo_NoAgregaMasPreferenciasQueElLimite()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        var mockGrupoRepo = new Mock<IGrupoRepositorio>();
        var mockZonaRepo = new Mock<IZonaRepositorio>();

        int limitePreferencias = 10;

        // Setup para DatosUsuarioSeteados
        mockGrupoRepo.Setup(r => r.DatosUsuarioSeteados(It.IsAny<int>()))
            .ReturnsAsync(true);

        // Setup para GuardarGrupo
        mockGrupoRepo.Setup(r => r.GuardarGrupo(It.IsAny<Grupo>()))
            .Callback<Grupo>(g => g.IdGrupo = 1) // Simula que EF asigna IdGrupo = 1
            .Returns(Task.CompletedTask);

        // Setup para ObtenerZonaPorId (se usa en setearZonasGrupo)
        mockZonaRepo.Setup(r => r.ObtenerZonaPorId(It.IsAny<int>()))
            .ReturnsAsync(new Zonas { Id = 1, Nombre = "Zona 1" });

        mockZonaRepo.Setup(r => r.GuardarZonaEnGrupo(It.IsAny<Zonas_Grupos>()))
            .Returns(Task.CompletedTask);

        // Setup para GuardarUsuarioGrupo
        mockGrupoRepo.Setup(r => r.GuardarUsuarioGrupo(It.IsAny<Usuario_Grupo>()))
            .Returns(Task.CompletedTask);

        // Setup para ObtenerIdPreferenciasGrupoPorTipo → previene NullReference
        mockGrupoRepo.Setup(r =>
            r.ObtenerIdPreferenciasGrupoPorTipo(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<int>()); // <- Esta línea es crucial

        // Setup para ObtenerPreferenciasGrupoPorTipo
        mockGrupoRepo.Setup(r =>
            r.ObtenerPreferenciasGrupoPorTipo(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Grupo_Preferencia>());

        // Setup para ObtenerIntegrantesGrupo
        var integrantes = new List<Usuario_Grupo>
    {
        new Usuario_Grupo { IdUsuario = 1 },
        new Usuario_Grupo { IdUsuario = 2 }
    };
        mockGrupoRepo.Setup(r => r.ObtenerIntegrantesGrupo(It.IsAny<int>()))
            .ReturnsAsync(integrantes);

        // Setup para ObtenerIdPreferenciasComunesOrdenadas
        mockGrupoRepo.Setup(r => r.ObtenerIdPreferenciasComunesOrdenadas(It.IsAny<int>(), It.IsAny<List<int>>()))
            .ReturnsAsync(Enumerable.Range(1, 20).ToList()); // 20 preferencias comunes

        // Setup para EliminarPreferenciasGrupoAuto
        mockGrupoRepo.Setup(r => r.EliminarPreferenciasGrupoAuto(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Setup para GuardarPreferenciaGrupo
        var preferenciasAgregadas = new List<Grupo_Preferencia>();
        mockGrupoRepo.Setup(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()))
            .Callback<Grupo_Preferencia>(gp => preferenciasAgregadas.Add(gp))
            .Returns(Task.CompletedTask);

        var grupoLogica = new GrupoLogica(mockGrupoRepo.Object, context, mockZonaRepo.Object);

        var grupoDTO = new GrupoInsertDTO
        {
            Nombre = "Grupo Test",
            Imagen = null,
            Zonas = new List<string> { "1" }
        };

        // Act
        var resultado = await grupoLogica.CrearGrupo(grupoDTO, 1);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(preferenciasAgregadas.Count <= limitePreferencias);
    }


    [Fact]
    public async Task CrearGrupo_EliminaPreferenciasAutomaticasAnterioresAntesDeAgregar()
    {
        var context = GetSqliteInMemoryDb();

        var usuario = new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" };
        context.Usuario.Add(usuario);
        await context.SaveChangesAsync();

        var mockGrupoRepo = new Mock<IGrupoRepositorio>();
        var mockZonaRepo = new Mock<IZonaRepositorio>();

        var grupoInsert = new GrupoInsertDTO { Nombre = "Grupo Preferencias", Zonas = new List<string> { "1" } };

        mockGrupoRepo.Setup(r => r.DatosUsuarioSeteados(usuario.IdUsuario)).ReturnsAsync(true);
        mockGrupoRepo.Setup(r => r.GuardarGrupo(It.IsAny<Grupo>())).Callback<Grupo>(g => g.IdGrupo = 99).Returns(Task.CompletedTask);

        mockGrupoRepo.Setup(r => r.ObtenerIdPreferenciasGrupoPorTipo(99, It.IsAny<int>())).ReturnsAsync(new List<int>());

        mockGrupoRepo.Setup(r => r.ObtenerIntegrantesGrupo(99)).ReturnsAsync(new List<Usuario_Grupo> { new Usuario_Grupo { IdUsuario = 1 } });
        mockGrupoRepo.Setup(r => r.ObtenerIdPreferenciasComunesOrdenadas(99, It.IsAny<List<int>>())).ReturnsAsync(new List<int> { 1, 2 });

        mockGrupoRepo.Setup(r => r.ObtenerPreferenciasGrupoPorTipo(99, It.IsAny<int>())).ReturnsAsync(new List<Grupo_Preferencia>());

        var eliminarLlamado = false;
        mockGrupoRepo.Setup(r => r.EliminarPreferenciasGrupoAuto(99)).Callback(() => eliminarLlamado = true).Returns(Task.CompletedTask);

        mockGrupoRepo.Setup(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>())).Returns(Task.CompletedTask);
        mockGrupoRepo.Setup(r => r.GuardarUsuarioGrupo(It.IsAny<Usuario_Grupo>())).Returns(Task.CompletedTask);

        mockZonaRepo.Setup(z => z.ObtenerZonaPorId(1)).ReturnsAsync(new Zonas { Id = 1, Nombre = "Zona 1" });
        mockZonaRepo.Setup(z => z.GuardarZonaEnGrupo(It.IsAny<Zonas_Grupos>())).Returns(Task.CompletedTask);

        var grupoLogica = new GrupoLogica(mockGrupoRepo.Object, context, mockZonaRepo.Object);

        var resultado = await grupoLogica.CrearGrupo(grupoInsert, usuario.IdUsuario);

        Assert.NotNull(resultado);
        Assert.True(eliminarLlamado, "EliminarPreferenciasGrupoAuto no fue llamado");
    }


    [Fact]
    public async Task CrearGrupo_DeberiaFallarSiHayMasDeTresZonas()
    {
        var context = GetSqliteInMemoryDb();

        // Insertamos un usuario y zonas en BD
        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        for (int i = 1; i <= 4; i++)
        {
            context.Zonas.Add(new Zonas { Id = i, Nombre = $"Zona{i}" });
        }
        await context.SaveChangesAsync();

        var mockGrupoRepo = new Mock<IGrupoRepositorio>();
        var mockZonaRepo = new Mock<IZonaRepositorio>();

        //simulamos que el usuario que tenga datos completos
        mockGrupoRepo.Setup(x => x.DatosUsuarioSeteados(1)).ReturnsAsync(true);

        // mock que devuelve la zona solicitada
        mockZonaRepo.Setup(z => z.ObtenerZonaPorId(It.IsAny<int>()))
            .ReturnsAsync((int id) => new Zonas { Id = id, Nombre = $"Zona{id}" });

        // los metodos de guardado no interesan para este test, basta que no llamen a la exception
        mockGrupoRepo.Setup(x => x.GuardarGrupo(It.IsAny<Grupo>())).Returns(Task.CompletedTask);
        mockZonaRepo.Setup(z => z.GuardarZonaEnGrupo(It.IsAny<Zonas_Grupos>())).Returns(Task.CompletedTask);
        mockGrupoRepo.Setup(x => x.GuardarUsuarioGrupo(It.IsAny<Usuario_Grupo>())).Returns(Task.CompletedTask);
        mockGrupoRepo.Setup(x => x.ObtenerIdPreferenciasGrupoPorTipo(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<int>());
        mockGrupoRepo.Setup(x => x.ObtenerIntegrantesGrupo(It.IsAny<int>())).ReturnsAsync(new List<Usuario_Grupo> { new Usuario_Grupo { IdUsuario = 1 } });
        mockGrupoRepo.Setup(x => x.ObtenerIdPreferenciasComunesOrdenadas(It.IsAny<int>(), It.IsAny<List<int>>())).ReturnsAsync(new List<int>());
        mockGrupoRepo.Setup(x => x.ObtenerPreferenciasGrupoPorTipo(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<Grupo_Preferencia>());
        mockGrupoRepo.Setup(x => x.EliminarPreferenciasGrupoAuto(It.IsAny<int>())).Returns(Task.CompletedTask);
        mockGrupoRepo.Setup(x => x.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>())).Returns(Task.CompletedTask);

        var grupoLogica = new GrupoLogica(mockGrupoRepo.Object, context, mockZonaRepo.Object);

        // creamos DTO con 4 zonas (el limite es 3)
        var dto = new GrupoInsertDTO
        {
            Nombre = "MuchoZonas",
            Zonas = new List<string> { "1", "2", "3", "4" },
            Imagen = null
        };

        var ex = await Assert.ThrowsAsync<Exception>(
            () => grupoLogica.CrearGrupo(dto, 1)
        );

        Assert.Contains("No se pueden agregar más de 3 zonas", ex.Message);
    }

    [Fact]
    public async Task CrearGrupo_DeberiaFallarSiImagenExtensionNoPermitida()
    {
        var context = GetSqliteInMemoryDb();

        // insertar un usuario y una zona válida
        context.Usuario.Add(new Usuario { IdUsuario = 1, Nombre = "Test", Email = "a@a.com", Password_Hash = "123" });
        context.Zonas.Add(new Zonas { Id = 1, Nombre = "Centro" });
        await context.SaveChangesAsync();

        var mockGrupoRepo = new Mock<IGrupoRepositorio>();
        var mockZonaRepo = new Mock<IZonaRepositorio>();

        mockGrupoRepo.Setup(x => x.DatosUsuarioSeteados(1)).ReturnsAsync(true);
        mockZonaRepo.Setup(z => z.ObtenerZonaPorId(1)).ReturnsAsync(new Zonas { Id = 1, Nombre = "Centro" });
        mockGrupoRepo.Setup(x => x.GuardarGrupo(It.IsAny<Grupo>())).Returns(Task.CompletedTask);

        // mock de guardado de zona/usuario para que no falle antes
        mockZonaRepo.Setup(z => z.GuardarZonaEnGrupo(It.IsAny<Zonas_Grupos>())).Returns(Task.CompletedTask);
        mockGrupoRepo.Setup(x => x.GuardarUsuarioGrupo(It.IsAny<Usuario_Grupo>())).Returns(Task.CompletedTask);

        // y mockeamos el resto del flujo de preferencias para que no se active (colecciones sin nada)
        mockGrupoRepo.Setup(x => x.ObtenerIdPreferenciasGrupoPorTipo(It.IsAny<int>(), It.IsAny<int>()))
                     .ReturnsAsync(new List<int>());
        mockGrupoRepo.Setup(x => x.ObtenerIntegrantesGrupo(It.IsAny<int>()))
                     .ReturnsAsync(new List<Usuario_Grupo> { new Usuario_Grupo { IdUsuario = 1 } });
        mockGrupoRepo.Setup(x => x.ObtenerIdPreferenciasComunesOrdenadas(It.IsAny<int>(), It.IsAny<List<int>>()))
                     .ReturnsAsync(new List<int>());
        mockGrupoRepo.Setup(x => x.ObtenerPreferenciasGrupoPorTipo(It.IsAny<int>(), It.IsAny<int>()))
                     .ReturnsAsync(new List<Grupo_Preferencia>());
        mockGrupoRepo.Setup(x => x.EliminarPreferenciasGrupoAuto(It.IsAny<int>()))
                     .Returns(Task.CompletedTask);
        mockGrupoRepo.Setup(x => x.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()))
                     .Returns(Task.CompletedTask);

        var grupoLogica = new GrupoLogica(mockGrupoRepo.Object, context, mockZonaRepo.Object);

        // Construimos un IFormFile con extensión .bmp (lo cual no se permite)
        var content = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));
        IFormFile invalidImage = new FormFile(content, 0, content.Length, "Data", "imagen_invalida.bmp");

        var dto = new GrupoInsertDTO
        {
            Nombre = "GrupoImagenInvalida",
            Zonas = new List<string> { "1" },
            Imagen = invalidImage
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => grupoLogica.CrearGrupo(dto, 1)
        );

        Assert.Contains("Formato de imagen no permitido", ex.Message);
    }


    /* Métodos privados */

    [Fact]
    public async Task SetearPreferenciasGrupoAuto_DeberiaAgregarPreferenciasComunes_SiHayEspacioDisponible()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();

        int idGrupo = 1;
        int idUsuario = 2;
        int valorPrioridadManual = 1;
        int valorPrioridadAuto = 0;
        int limitePreferencias = 3;

        grupoRepoMock.Setup(r => r.ObtenerIdPreferenciasGrupoPorTipo(idGrupo, valorPrioridadManual))
                     .ReturnsAsync(new List<int>()); // no hay preferencias manuales

        grupoRepoMock.Setup(r => r.ObtenerIntegrantesGrupo(idGrupo))
                     .ReturnsAsync(new List<Usuario_Grupo> {
                     new Usuario_Grupo { IdUsuario = 2 },
                     new Usuario_Grupo { IdUsuario = 3 }
                     });

        grupoRepoMock.Setup(r => r.ObtenerIdPreferenciasComunesOrdenadas(idGrupo, It.IsAny<List<int>>()))
                     .ReturnsAsync(new List<int> { 10, 20, 30 }); // preferencias comunes

        grupoRepoMock.Setup(r => r.ObtenerPreferenciasGrupoPorTipo(idGrupo, valorPrioridadAuto))
                     .ReturnsAsync(new List<Grupo_Preferencia>()); // no hay preferencias automáticas

        grupoRepoMock.Setup(r => r.EliminarPreferenciasGrupoAuto(idGrupo)).Returns(Task.CompletedTask);

        var preferenciasGuardadas = new List<Grupo_Preferencia>();
        grupoRepoMock.Setup(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()))
                     .Callback<Grupo_Preferencia>(p => preferenciasGuardadas.Add(p))
                     .Returns(Task.CompletedTask);

        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        // Act (invocar método privado por reflection)
        var metodo = typeof(GrupoLogica).GetMethod("setearPreferenciasGrupoAuto", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task) metodo.Invoke(grupoLogica, new object[] { idGrupo, idUsuario });
        await task;

        // Assert
        Assert.Equal(3, preferenciasGuardadas.Count); // Se agregaron 3 preferencias comunes
        Assert.Contains(preferenciasGuardadas, p => p.IdPreferencia == 10);
        Assert.Contains(preferenciasGuardadas, p => p.IdPreferencia == 20);
        Assert.Contains(preferenciasGuardadas, p => p.IdPreferencia == 30);
        Assert.All(preferenciasGuardadas, p => Assert.Equal(0, p.Prioridad)); // Prioridad automática
    }

    // En este de aca, probamos que funcione poner preferencias automaticas si no hay manuales

    [Fact]
    public async Task SetearPreferenciasGrupoAuto_NoHaceNadaSiYaHaySuficientesPreferenciasManuales()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();

        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);
        int idGrupo = 1;
        int idUsuario = 10;

        var preferenciasManuales = new List<int> { 1, 2, 3, 4, 5 };

        grupoRepoMock.Setup(r => r.ObtenerIdPreferenciasGrupoPorTipo(idGrupo, 1)) // 1 = agregadas manual
            .ReturnsAsync(preferenciasManuales);

        // Act
        var metodoPrivado = typeof(GrupoLogica).GetMethod("setearPreferenciasGrupoAuto", BindingFlags.Instance | BindingFlags.NonPublic);
        await (Task)metodoPrivado.Invoke(grupoLogica, new object[] { idGrupo, idUsuario });

        // Assert - verificar que NO se llamaron a métodos que modifican
        grupoRepoMock.Verify(r => r.EliminarPreferenciasGrupoAuto(It.IsAny<int>()), Times.Never);
        grupoRepoMock.Verify(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()), Times.Never);
    }

    // En este de acá, se busca que se rellenen con preferencias en comun de otros integrantes si no alcanza el limite, descartando las automaticas

    [Fact]
    public async Task SetearPreferenciasGrupoAuto_DeberiaAgregarPreferenciasAutoSiHayDisponibles()
    {
        // Arrange
        using var context = GetSqliteInMemoryDb();
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();

        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        int idGrupo = 1;
        int idUsuario = 10;

        // Menos de 5 preferencias manuales
        grupoRepoMock.Setup(r => r.ObtenerIdPreferenciasGrupoPorTipo(idGrupo, 1))
                     .ReturnsAsync(new List<int> { 1, 2 });

        // Integrantes del grupo
        grupoRepoMock.Setup(r => r.ObtenerIntegrantesGrupo(idGrupo))
                     .ReturnsAsync(new List<Usuario_Grupo>
                     {
                     new Usuario_Grupo { IdUsuario = 10 },
                     new Usuario_Grupo { IdUsuario = 11 }
                     });

        // Preferencias comunes entre integrantes
        grupoRepoMock.Setup(r => r.ObtenerIdPreferenciasComunesOrdenadas(idGrupo, It.IsAny<List<int>>()))
                     .ReturnsAsync(new List<int> { 3, 4, 5 });

        // Ya existen 2 automáticas
        grupoRepoMock.Setup(r => r.ObtenerPreferenciasGrupoPorTipo(idGrupo, 0))
                     .ReturnsAsync(new List<Grupo_Preferencia>
                     {
                     new Grupo_Preferencia { IdPreferencia = 9 },
                     new Grupo_Preferencia { IdPreferencia = 10 }
                     });

        grupoRepoMock.Setup(r => r.EliminarPreferenciasGrupoAuto(idGrupo))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

        grupoRepoMock.Setup(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

        // Act
        var metodoPrivado = typeof(GrupoLogica).GetMethod("setearPreferenciasGrupoAuto", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)metodoPrivado.Invoke(grupoLogica, new object[] { idGrupo, idUsuario });

        // Assert
        grupoRepoMock.Verify(r => r.EliminarPreferenciasGrupoAuto(idGrupo), Times.Once);
        grupoRepoMock.Verify(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()), Times.Exactly(3)); // Agrega 3 para llegar a 5
    }

    // En este de aca, se eliminan las preferencias automaticas, y no hay manuales para agregar

    [Fact]
    public async Task SetearPreferenciasGrupoAuto_DeberiaEliminarPreferenciasPeroNoAgregarSiNoHayComunes()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        int idGrupo = 3;
        int idUsuario = 100;

        // Preferencias agregadas manualmente (solo 1, así que se pueden agregar automáticas)
        grupoRepoMock.Setup(r => r.ObtenerIdPreferenciasGrupoPorTipo(idGrupo, 1))
                     .ReturnsAsync(new List<int> { 1 });

        // Integrantes del grupo
        grupoRepoMock.Setup(r => r.ObtenerIntegrantesGrupo(idGrupo))
                     .ReturnsAsync(new List<Usuario_Grupo>
                     {
                     new Usuario_Grupo { IdUsuario = 101 },
                     new Usuario_Grupo { IdUsuario = 102 }
                     });

        // NO hay preferencias comunes
        grupoRepoMock.Setup(r => r.ObtenerIdPreferenciasComunesOrdenadas(idGrupo, It.IsAny<List<int>>()))
                     .ReturnsAsync(new List<int>());

        // Ya existen preferencias automáticas
        grupoRepoMock.Setup(r => r.ObtenerPreferenciasGrupoPorTipo(idGrupo, 0))
                     .ReturnsAsync(new List<Grupo_Preferencia>
                     {
                     new Grupo_Preferencia { IdGrupo = idGrupo, IdPreferencia = 8 }
                     });

        // Se espera que se eliminen
        grupoRepoMock.Setup(r => r.EliminarPreferenciasGrupoAuto(idGrupo))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

        // Pero no se debe llamar a guardar nuevas preferencias
        grupoRepoMock.Setup(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

        // Act
        var metodoPrivado = typeof(GrupoLogica).GetMethod("setearPreferenciasGrupoAuto", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)metodoPrivado.Invoke(grupoLogica, new object[] { idGrupo, idUsuario });

        // Assert
        grupoRepoMock.Verify(r => r.EliminarPreferenciasGrupoAuto(idGrupo), Times.Once);
        grupoRepoMock.Verify(r => r.GuardarPreferenciaGrupo(It.IsAny<Grupo_Preferencia>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarZonasGrupo_DeberiaVaciarZonasYGuardarZonasNuevas()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        var grupo = new Grupo { IdGrupo = 10 };
        var zona1 = new Zonas { Id = 1, Nombre = "Centro" };
        var zona2 = new Zonas { Id = 2, Nombre = "Norte" };

        var zonasDto = new List<ZonaDTO>
    {
        new ZonaDTO { Id = zona1.Id },
        new ZonaDTO { Id = zona2.Id }
    };

        var grupoDto = new GrupoUpdateDTO
        {
            ZonasJson = JsonConvert.SerializeObject(zonasDto)
        };

        zonaRepoMock.Setup(z => z.ObtenerZonaPorId(zona1.Id)).ReturnsAsync(zona1);
        zonaRepoMock.Setup(z => z.ObtenerZonaPorId(zona2.Id)).ReturnsAsync(zona2);
        zonaRepoMock.Setup(z => z.GuardarZonaEnGrupo(It.IsAny<Zonas_Grupos>())).Returns(Task.CompletedTask).Verifiable();
        grupoRepoMock.Setup(g => g.VaciarZonasDelGrupo(grupo)).Returns(Task.CompletedTask).Verifiable();

        // Act
        var metodoPrivado = typeof(GrupoLogica).GetMethod("actualizarZonasGrupo", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)metodoPrivado.Invoke(grupoLogica, new object[] { grupo, grupoDto });

        // Assert
        grupoRepoMock.Verify(g => g.VaciarZonasDelGrupo(grupo), Times.Once);
        zonaRepoMock.Verify(z => z.GuardarZonaEnGrupo(It.Is<Zonas_Grupos>(zg => zg.IdZona == zona1.Id && zg.IdGrupo == grupo.IdGrupo)), Times.Once);
        zonaRepoMock.Verify(z => z.GuardarZonaEnGrupo(It.Is<Zonas_Grupos>(zg => zg.IdZona == zona2.Id && zg.IdGrupo == grupo.IdGrupo)), Times.Once);
    }

    [Fact]
    public void ValidarActualizacionPreferencias_DeberiaRetornarFalse_SiSuperaElLimite()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        var preferencias = new List<PreferenciaDTO>
    {
        new PreferenciaDTO { Prioridad = 0 },
        new PreferenciaDTO { Prioridad = 1 },
        new PreferenciaDTO { Prioridad = 0 },
        new PreferenciaDTO { Prioridad = 1 },
        new PreferenciaDTO { Prioridad = 0 },
        new PreferenciaDTO { Prioridad = 1 } // Supera el límite de 5
    };

        // Act
        var metodoPrivado = typeof(GrupoLogica).GetMethod("validarActualizacionPreferencias", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var resultado = (bool)metodoPrivado.Invoke(grupoLogica, new object[] { preferencias });

        // Assert
        Assert.False(resultado);
    }

    
    [Fact]
    public void ValidarActualizacionPreferencias_DeberiaRetornarTrue_SiEsMenorOIgualAlLimite()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        var preferencias = new List<PreferenciaDTO>
    {
        new PreferenciaDTO { Prioridad = 0 },
        new PreferenciaDTO { Prioridad = 1 },
        new PreferenciaDTO { Prioridad = 0 },
        new PreferenciaDTO { Prioridad = 1 },
        new PreferenciaDTO { Prioridad = 0 } // Total = 5
    };

        // Act
        var metodoPrivado = typeof(GrupoLogica).GetMethod("validarActualizacionPreferencias", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var resultado = (bool)metodoPrivado.Invoke(grupoLogica, new object[] { preferencias });

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void CrearGrupoDTO_DeberiaIgnorarElementosConPropiedadesNavigationNulas()
    {
        // Arrange
        var context = GetSqliteInMemoryDb();
        var grupoRepoMock = new Mock<IGrupoRepositorio>();
        var zonaRepoMock = new Mock<IZonaRepositorio>();
        var grupoLogica = new GrupoLogica(grupoRepoMock.Object, context, zonaRepoMock.Object);

        var campo = typeof(GrupoLogica).GetField("valorPrioridadEliminaManual", BindingFlags.NonPublic | BindingFlags.Instance);

        if (campo == null)
            throw new Exception("No se encontró el campo 'valorPrioridadEliminaManual' en GrupoLogica");

        var prioridadEliminada = (int)campo.GetValue(grupoLogica)!;

        var grupo = new Grupo
        {
            IdGrupo = 1,
            Nombre = "Juntada NullTest",
            UrlImagen = "imagen.png",
            TokenInvitacion = "TOKEN",
            Activo = true,

            Zonas_Grupos = new List<Zonas_Grupos>
        {
            new Zonas_Grupos
            {
                IdZona = 10,
                IdZonaNavigation = new Zonas { Id = 10, Nombre = "Centro" }
            },
            new Zonas_Grupos
            {
                IdZona = 99,
                IdZonaNavigation = null
            }
        },

            Usuario_Grupo = new List<Usuario_Grupo>
        {
            new Usuario_Grupo
            {
                IdUsuario = 100,
                Administrador = true,
                IdUsuarioNavigation = new Usuario { IdUsuario = 100, Nombre = "Lucía" }
            },
            new Usuario_Grupo
            {
                IdUsuario = 999,
                Administrador = false,
                IdUsuarioNavigation = null
            }
        },

            Grupo_Preferencia = new List<Grupo_Preferencia>
        {
            new Grupo_Preferencia
            {
                IdPreferencia = 200,
                Prioridad = 1,
                IdPreferenciaNavigation = new Preferencia { IdPreferencia = 200, Nombre = "Pizzas" }
            },
            new Grupo_Preferencia
            {
                IdPreferencia = 201,
                Prioridad = prioridadEliminada,
                IdPreferenciaNavigation = new Preferencia { IdPreferencia = 201, Nombre = "Ignorada" }
            },
            new Grupo_Preferencia
            {
                IdPreferencia = 202,
                Prioridad = 1,
                IdPreferenciaNavigation = null
            }
        }
        };

        try
        {
            // Act
            var metodoPrivado = typeof(GrupoLogica).GetMethod("CrearGrupoDTO", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var resultado = (GrupoDTO)metodoPrivado.Invoke(grupoLogica, new object[] { grupo })!;

            // Assert
            Assert.Equal(1, resultado.IdGrupo);
            Assert.Equal("Juntada NullTest", resultado.Nombre);

            Assert.Single(resultado.Zonas);
            Assert.Equal("Centro", resultado.Zonas[0].Nombre);

            Assert.Single(resultado.Integrantes);
            Assert.Equal("Lucía", resultado.Integrantes[0].Nombre);

            Assert.Single(resultado.Preferencias);
            Assert.Equal("Pizzas", resultado.Preferencias[0].Nombre);
        }
        catch (NullReferenceException ex)
        {
            // Mostrá información para detectar el campo que falla
            var zonasList = grupo.Zonas_Grupos.ToList();
            var usuariosList = grupo.Usuario_Grupo.ToList();
            var preferenciasList = grupo.Grupo_Preferencia.ToList();

            Console.WriteLine("Zonas_Grupos[1].Navigation null? " + (zonasList.Count > 1 && zonasList[1].IdZonaNavigation == null));
            Console.WriteLine("Usuario_Grupo[1].Navigation null? " + (usuariosList.Count > 1 && usuariosList[1].IdUsuarioNavigation == null));
            Console.WriteLine("Grupo_Preferencia[1].Navigation null? " + (preferenciasList.Count > 1 && preferenciasList[1].IdPreferenciaNavigation == null));
            Console.WriteLine("Grupo_Preferencia[2].Navigation null? " + (preferenciasList.Count > 2 && preferenciasList[2].IdPreferenciaNavigation == null));

            throw;
        }
    }

    [Fact]
    public async Task GuardarImagenAsync_DeberiaGuardarArchivoYRetornarRuta_ConReflexion()
    {
        // Arrange
        var grupoLogica = CrearGrupoLogicaConMocks();

        var metodoPrivado = typeof(GrupoLogica)
            .GetMethod("GuardarImagenAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        var contenido = "contenido de prueba";
        var nombreArchivo = "imagen.png";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contenido));
        var formFile = new FormFile(stream, 0, stream.Length, "file", nombreArchivo)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        // Act
        var task = (Task<string>)metodoPrivado!.Invoke(grupoLogica, new object[] { formFile });
        var resultado = await task;

        // Assert
        Assert.StartsWith("/uploads/grupos/", resultado);
    }

    [Fact]
    public async Task GuardarImagenAsync_DeberiaRetornarRutaDefault_SiImagenEsNull()
    {
        // Arrange
        var grupoLogica = CrearGrupoLogicaConMocks();

        var metodoPrivado = typeof(GrupoLogica)
            .GetMethod("GuardarImagenAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(metodoPrivado); // Verifica que se encontró el método

        // Act
        var task = (Task<string>)metodoPrivado.Invoke(grupoLogica, new object?[] { null })!;
        var resultado = await task;

        // Assert
        Assert.Equal("/avatar-group/default.png", resultado);
    }

    [Fact]
    public async Task GuardarImagenAsync_DeberiaLanzarExcepcion_SiExtensionNoEsValida()
    {
        // Arrange
        var grupoLogica = CrearGrupoLogicaConMocks();

        var metodoPrivado = typeof(GrupoLogica)
            .GetMethod("GuardarImagenAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(metodoPrivado);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("malicioso.exe");

        // Act
        var task = (Task<string>)metodoPrivado.Invoke(grupoLogica, new object?[] { mockFile.Object })!;

        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => task);
        Assert.Equal("Formato de imagen no permitido.", exception.Message);
    }

    [Fact]
    public async Task GuardarImagenAsync_DeberiaGuardarImagenYRetornarRutaCorrecta_SiExtensionEsValida()
    {
        // Arrange
        var grupoLogica = CrearGrupoLogicaConMocks();

        var metodoPrivado = typeof(GrupoLogica)
            .GetMethod("GuardarImagenAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(metodoPrivado);

        // Simulamos un archivo PNG en memoria
        var content = "fake image content";
        var fileName = "foto.png";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns<Stream, CancellationToken>((s, _) => stream.CopyToAsync(s));

        // Act
        var task = (Task<string>)metodoPrivado.Invoke(grupoLogica, new object?[] { mockFile.Object })!;
        var resultadoRuta = await task;

        // Assert
        Assert.StartsWith("/uploads/grupos/", resultadoRuta); // Verifica que la ruta sea la esperada

        // Además, podrías verificar que el archivo exista físicamente (opcional)
        var rutaFisica = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", resultadoRuta.TrimStart('/'));
        Assert.True(File.Exists(rutaFisica));

        // Cleanup (eliminamos archivo de test)
        File.Delete(rutaFisica);
    }
}
