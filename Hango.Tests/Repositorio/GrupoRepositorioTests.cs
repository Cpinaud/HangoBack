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

public class GrupoRepositorioTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestHangoContext _context;
    private readonly GrupoRepositorio _repo;

    public GrupoRepositorioTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<HangoContext>()
            .UseSqlite(_connection)
            .ConfigureWarnings(x => x.Ignore(RelationalEventId.AmbientTransactionWarning))
            .Options;

        _context = new TestHangoContext(options);
        _context.Database.EnsureCreated();

        _repo = new GrupoRepositorio(_context);
    }

    private class TestHangoContext : HangoContext
    {
        public TestHangoContext(DbContextOptions<HangoContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Removemos cualquier DefaultValue o DefaultValueSql si estaba en el modelo original
           /*
            modelBuilder.Entity<Grupo>()
                .Property(e => e.CreateAt)
                .Metadata.SetDefaultValue(null);

            modelBuilder.Entity<Usuario>()
                .Property(e => e.CreateAt)
                .Metadata.SetDefaultValue(null);

            modelBuilder.Entity<Cuenta>()
                .Property(e => e.FechaCreacion)
                .Metadata.SetDefaultValue(null);

            */

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

    public void Dispose()
    {
        _context?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }

    [Fact]
    public async Task DatosUsuarioSeteados_DeberiaRetornarTrue_CuandoTienePreferenciasYHorarios()
    {
        var usuario = new Usuario
        {
            IdUsuario = 1,
            Nombre = "Test Usuario",
            Email = "test@test.com",
            Password_Hash = "hash",
            IdAvatar = 1,
            EstaVerificado = true,
            CreateAt = DateTime.UtcNow
        };
        _context.Usuario.Add(usuario);
        await _context.SaveChangesAsync();

        _context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Test Preferencia" });
        _context.Usuario_Preferencia.Add(new Usuario_Preferencia { IdUsuario = 1, IdPreferencia = 1 });
        _context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = 1,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(10, 0, 0),
            HorarioFin = new TimeSpan(12, 0, 0)
        });

        await _context.SaveChangesAsync();

        var resultado = await _repo.DatosUsuarioSeteados(1);
        Assert.True(resultado);
    }

    [Fact]
    public async Task DatosUsuarioSeteados_DeberiaRetornarFalse_CuandoFaltanPreferencias()
    {
        _context.Usuario.Add(new Usuario
        {
            IdUsuario = 1,
            Nombre = "Test Usuario",
            Email = "test@test.com",
            Password_Hash = "hash",
            IdAvatar = 1,
            EstaVerificado = true,
            CreateAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _context.HorarioDisponible.Add(new HorarioDisponible
        {
            IdUsuario = 1,
            Fecha = DateTime.Today,
            DiaSemana = (byte)DateTime.Today.DayOfWeek,
            HorarioInicio = new TimeSpan(10, 0, 0),
            HorarioFin = new TimeSpan(12, 0, 0)
        });

        await _context.SaveChangesAsync();

        var resultado = await _repo.DatosUsuarioSeteados(1);
        Assert.False(resultado);
    }

    [Fact]
    public async Task DatosUsuarioSeteados_DeberiaRetornarFalse_CuandoFaltanHorarios()
    {
        _context.Usuario.Add(new Usuario
        {
            IdUsuario = 1,
            Nombre = "Test Usuario",
            Email = "test@test.com",
            Password_Hash = "hash",
            IdAvatar = 1,
            EstaVerificado = true,
            CreateAt = DateTime.UtcNow
        });

        _context.Preferencia.Add(new Preferencia { IdPreferencia = 1, Nombre = "Test Preferencia" });
        _context.Usuario_Preferencia.Add(new Usuario_Preferencia { Id = 1, IdUsuario = 1, IdPreferencia = 1 });
        await _context.SaveChangesAsync();

        var resultado = await _repo.DatosUsuarioSeteados(1);
        Assert.False(resultado);
    }

    [Fact]
    public async Task DatosUsuarioSeteados_DeberiaRetornarFalse_CuandoNoTienePreferenciasNiHorarios()
    {
        _context.Usuario.Add(new Usuario
        {
            IdUsuario = 1,
            Nombre = "Test Usuario",
            Email = "test@test.com",
            Password_Hash = "hash",
            IdAvatar = 1,
            EstaVerificado = true,
            CreateAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var resultado = await _repo.DatosUsuarioSeteados(1);
        Assert.False(resultado);
    }
}
