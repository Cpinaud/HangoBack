using Hango.Datos.DTO.Horario;
using Hango.Datos.EF;
using Hango.Repositorios.Horario;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Tests.Repositorio.Horario
{
    public class HorarioDisponibleRepositorioTest
    {
        private readonly Mock<HangoContext> _mockContext;
        private readonly Mock<DbSet<HorarioDisponible>> _mockDbSetHorario;
        private readonly HorarioDisponibleRepositorio _repositorio;

        public HorarioDisponibleRepositorioTest()
        {
            _mockContext = new Mock<HangoContext>();
            _mockDbSetHorario = new Mock<DbSet<HorarioDisponible>>();
            _mockContext
                .Setup(c => c.HorarioDisponible)
                .Returns(_mockDbSetHorario.Object);

            _repositorio = new HorarioDisponibleRepositorio(_mockContext.Object);
        }

      [Fact]
    public async Task ActualizarHorariosUsuarioAsync_RemueveYGuarda()
        {
            var originales = new List<HorarioDisponible>
        {
        new HorarioDisponible { IdHorarioDisponible = 10 },
        new HorarioDisponible { IdHorarioDisponible = 20 }
        };
            var usuario = new Datos.EF.Usuario
            {
                IdUsuario = 1,
                HorarioDisponible = originales
            };
            var nuevos = new List<HorarioDisponibleDTO>
        {
        new HorarioDisponibleDTO { Dia = "1", Fecha = DateTime.Today, HoraInicio = TimeSpan.FromHours(9), HoraFin = TimeSpan.FromHours(12) }
        };

            _mockDbSetHorario
                .Setup(d => d.RemoveRange(originales));
            _mockContext
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _repositorio.ActualizarHorariosUsuarioAsync(usuario, nuevos);
            _mockDbSetHorario.Verify(d => d.RemoveRange(originales), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.Single(usuario.HorarioDisponible);
        }

        [Fact]
        public async Task ActualizarHorariosUsuarioAsync_CuandoGuardaLanzaExcepcion()
        {
            var usuario = new Datos.EF.Usuario { IdUsuario = 2, HorarioDisponible = new List<HorarioDisponible>() };
            var nuevos = new List<HorarioDisponibleDTO>();

            _mockDbSetHorario
                .Setup(d => d.RemoveRange(It.IsAny<IEnumerable<HorarioDisponible>>()));
            _mockContext
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Error interno"));

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _repositorio.ActualizarHorariosUsuarioAsync(usuario, nuevos));

            Assert.Contains("Error al actualizar los datos", ex.Message);
        }
        [Fact]
        public async Task GuardarAsync_AgregaYRetornaEntidad()
        {
            var horario = new HorarioDisponible { IdHorarioDisponible = 5, IdUsuario = 3 };

            _mockDbSetHorario
                .Setup(d => d.AddAsync(horario, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<HorarioDisponible>)null);

            var resultado = await _repositorio.GuardarAsync(horario);

            Assert.Same(horario, resultado);
            _mockDbSetHorario.Verify(d => d.AddAsync(horario, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GuardarAsync_CuandoFallaLanzaExcepcion()
        {
            var horario = new HorarioDisponible();

            _mockDbSetHorario
                .Setup(d => d.AddAsync(It.IsAny<HorarioDisponible>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("boom"));

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _repositorio.GuardarAsync(horario));

            Assert.Contains("Error al actualizar los datos", ex.Message);
        }
        [Fact]
        public async Task ObtenerHorarioDisponiblePorUsuario_DevuelveSoloLosDelUsuario()
        {
            var h1 = new HorarioDisponible { IdHorarioDisponible = 1, IdUsuario = 10 };
            var h2 = new HorarioDisponible { IdHorarioDisponible = 2, IdUsuario = 20 };
            using var ctx = CreateInMemoryContext(h1, h2);
            var repo = new HorarioDisponibleRepositorio(ctx);

            var lista = await repo.ObtenerHorarioDisponiblePorUsuario(10);

            Assert.Single(lista);
            Assert.Equal(10, lista[0].IdUsuario);
        }

        [Fact]
        public async Task ObtenerHorarioDisponiblePorUsuario_SinDatosRetornaVacio()
        {
            using var ctx = CreateInMemoryContext();
            var repo = new HorarioDisponibleRepositorio(ctx);

            var lista = await repo.ObtenerHorarioDisponiblePorUsuario(999);
            Assert.Empty(lista);
        }

        

        [Fact]
        public async Task ObtenerPorUsuariosAsync_DevuelveLosCorrectos()
        {
            var h1 = new HorarioDisponible { IdHorarioDisponible = 1, IdUsuario = 5 };
            var h2 = new HorarioDisponible { IdHorarioDisponible = 2, IdUsuario = 6 };
            var h3 = new HorarioDisponible { IdHorarioDisponible = 3, IdUsuario = 5 };
            using var ctx = CreateInMemoryContext(h1, h2, h3);
            var repo = new HorarioDisponibleRepositorio(ctx);

            var lista = await repo.ObtenerPorUsuariosAsync(new List<int> { 5, 7 });

            Assert.Equal(2, lista.Count);
            Assert.All(lista, h => Assert.Equal(5, h.IdUsuario));
        }

        [Fact]
        public async Task ObtenerPorUsuariosAsync_SinUsuariosRetornaVacio()
        {
            using var ctx = CreateInMemoryContext();
            var repo = new HorarioDisponibleRepositorio(ctx);

            var lista = await repo.ObtenerPorUsuariosAsync(new List<int> { 123 });
            Assert.Empty(lista);
        }
        private HangoContext CreateInMemoryContext(params HorarioDisponible[] items)
        {
            var opts = new DbContextOptionsBuilder<HangoContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new HangoContext(opts);
            ctx.HorarioDisponible.AddRange(items);
            ctx.SaveChanges();
            return ctx;
        }
    }
}
