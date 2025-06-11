using Hango.Datos.EF;
using Hango.Repositorios.Cuenta;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Tests.Repositorio.Cuenta
{
    public class CuentaRepositorioTest
    {
        private readonly Mock<HangoContext> _mockContext;
        private readonly Mock<DbSet<ParticipacionCuenta>> _mockDbSet;
        private readonly CuentaRepositorio _repositorio;
        private readonly Mock<DbSet<Hango.Datos.EF.Cuenta>> _mockDbSetCuenta;

        public CuentaRepositorioTest()
        {
            _mockContext = new Mock<HangoContext>();
            _mockDbSet = new Mock<DbSet<ParticipacionCuenta>>();
            _mockDbSetCuenta = new Mock<DbSet<Hango.Datos.EF.Cuenta>>();

            _mockContext.Setup(c => c.ParticipacionCuenta).Returns(_mockDbSet.Object);
            _mockContext.Setup(c => c.Cuenta).Returns(_mockDbSetCuenta.Object);

            _repositorio = new CuentaRepositorio(_mockContext.Object);
        }

        [Fact]
        public async Task ActualizarParticipacionesAsync_FallaElGuardarLanzaExcepcion()
        {
            var participaciones = new List<ParticipacionCuenta>
            {
                new ParticipacionCuenta { CuentaId = 1, UsuarioId = 10 }
            };
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new DbUpdateException("Error en la BD"));

            var ex = await Assert.ThrowsAsync<Exception>(() =>
            _repositorio.ActualizarParticipacionesAsync(participaciones));
            Assert.Contains("Error al actualizar los datos", ex.Message);
        }

        [Fact]
        public async Task ActualizarParticipacionesAsync_ActualizaListaYGuarda()
        {
            var participaciones = new List<ParticipacionCuenta>
        {
            new ParticipacionCuenta { CuentaId = 1, UsuarioId = 10 },
            new ParticipacionCuenta { CuentaId = 1, UsuarioId = 20 }
        };
            await _repositorio.ActualizarParticipacionesAsync(participaciones);
            _mockDbSet.Verify(d => d.UpdateRange(participaciones), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ActualizarParticipacionAsync_FallaElGuardarLanzaExcepcion()
        {
            var participacion = new ParticipacionCuenta { CuentaId = 1, UsuarioId = 2 };
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                         .ThrowsAsync(new DbUpdateException("Error guardando"));

            var ex = await Assert.ThrowsAsync<Exception>(() =>
            _repositorio.ActualizarParticipacionAsync(participacion));
            Assert.Contains("Error al actualizar los datos", ex.Message);
        }
        [Fact]
        public async Task ActualizarParticipacionAsync_CuandoEsValido_ActualizaYGuarda()
        {
            var participacion = new ParticipacionCuenta { CuentaId = 1, UsuarioId = 2 };

            await _repositorio.ActualizarParticipacionAsync(participacion);
            _mockDbSet.Verify(d => d.Update(participacion), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CerrarCuentaAsync_CuandoFallaLanzaExcepcion()
        {
           
            var cuenta = new Hango.Datos.EF.Cuenta { IdCuenta = 1 };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Error cerrando cuenta"));

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _repositorio.CerrarCuentaAsync(cuenta));

            Assert.Contains("Error al actualizar los datos", ex.Message); 
        }

        [Fact]
        public async Task CerrarCuentaAsync_ActualizYCierraLaCuenta()
        {
            var cuenta = new Hango.Datos.EF.Cuenta { IdCuenta = 1 };

            await _repositorio.CerrarCuentaAsync(cuenta);
            _mockDbSetCuenta.Verify(d => d.Update(cuenta), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CrearCuentaAsync_CuandoFallaLanzaExcepcion()
        {
           
            var cuenta = new Hango.Datos.EF.Cuenta { IdCuenta = 1 };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Error creando cuenta"));

         
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _repositorio.CrearCuentaAsync(cuenta));

            Assert.Contains("Error al actualizar los datos", ex.Message); // de RepositoryHelper
        }

        [Fact]
        public async Task CrearCuentaAsync_CuandoEsExitoso_RetornaIdCuenta()
        {
            

            var cuenta = new Hango.Datos.EF.Cuenta { IdCuenta = 1 };

            _mockContext.Setup(c => c.Cuenta.Add(It.IsAny<Hango.Datos.EF.Cuenta>())); 
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); 

            var result = await _repositorio.CrearCuentaAsync(cuenta);

            Assert.Equal(1, result);
        }
        [Fact]
        public async Task FindCuentaById_NoExisteLaCuentaLanzaException()
        {


            _mockDbSetCuenta
                .Setup(d => d.FindAsync(5))
                .ReturnsAsync((Datos.EF.Cuenta)null);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _repositorio.FindCuentaById(5));

            Assert.Equal("Cuenta no encontrada", ex.Message);
        }
        [Fact]
        public async Task FindCuentaById_ExisteDevuelveEntidad()
        {
            var expected = new Datos.EF.Cuenta { IdCuenta = 1 };
            
            _mockDbSetCuenta
                .Setup(d => d.FindAsync(1))
                .ReturnsAsync(expected);

            var actual = await _repositorio.FindCuentaById(1);

            Assert.Same(expected, actual);
        }
        [Fact]
        public async Task ObtenerParticipacionesPorCuentaAsync_SinResultados_RetornaListaVacia()
        {
            using var ctx = CreateInMemoryContextWithParticipaciones();
            var repo = new CuentaRepositorio(ctx);

            var list = await repo.ObtenerParticipacionesPorCuentaAsync(999);

            Assert.Empty(list);
        }
        [Fact]
         public async Task ObtenerParticipacionesPorCuentaAsync_DevuelveSoloSuCuenta()
        {
            var p1 = new ParticipacionCuenta { CuentaId = 1, UsuarioId = 10, Estado = "Pendiente" };
            var p2 = new ParticipacionCuenta { CuentaId = 2, UsuarioId = 20 , Estado = "Pendiente" };
            using var ctx = CreateInMemoryContextWithParticipaciones(p1, p2);
            var repo = new CuentaRepositorio(ctx);

            var list = await repo.ObtenerParticipacionesPorCuentaAsync(1);

            Assert.Single(list);
            Assert.Equal(10, list[0].UsuarioId);
        }

        [Fact]
        public async Task ObtenerParticipacionPorCuentaYUsuarioAsync_ExisteDevuelveEntidad()
        {
            var item = new ParticipacionCuenta { CuentaId = 5, UsuarioId = 7, Estado = "Pendiente" };
            using var ctx = CreateInMemoryContextWithParticipaciones(item);
            var repo = new CuentaRepositorio(ctx);

            var found = await repo.ObtenerParticipacionPorCuentaYUsuarioAsync(5, 7);

            Assert.Equal(7, found.UsuarioId);
        }

        [Fact]
        public async Task ObtenerParticipacionPorCuentaYUsuarioAsync_NoExiste_LanzaException()
        {
            using var ctx = CreateInMemoryContextWithParticipaciones();
            var repo = new CuentaRepositorio(ctx);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                repo.ObtenerParticipacionPorCuentaYUsuarioAsync(1, 1));

            Assert.Equal("Cuenta no encontrada", ex.Message);
        }
        [Fact]
        public async Task ObtenerParticipantesDeUnaCuentaAsync_MapeaCorrectamenteDTO()
        {
 
            var usuario = new Datos.EF.Usuario { IdUsuario = 8, Nombre = "Juan", Email = "juan@email.com", Password_Hash = "pass" };
            var tipo = new TipoParticipacion { IdTipoParticipacion = 2, Nombre = "Individual" };
            var pc = new ParticipacionCuenta
            {
                CuentaId = 42,
                UsuarioId = 8,
                Usuario = usuario,
                IdTipoParticipacionNavigation = tipo,
                Estado = "P",
                MontoDebe = 100m,
                MontoGastado = 200m
            };

            var opts = new DbContextOptionsBuilder<HangoContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var ctx = new HangoContext(opts);
            ctx.Usuario.Add(usuario);
            ctx.TipoParticipacion.Add(tipo);
            ctx.ParticipacionCuenta.Add(pc);
            ctx.SaveChanges();

            var repo = new CuentaRepositorio(ctx);
            var dtos = await repo.ObtenerParticipantesDeUnaCuentaAsync(42);

            Assert.Single(dtos);
            var dto = dtos[0];
            Assert.Equal(8, dto.IdUsuario);
            Assert.Equal("Juan", dto.Nombre);
            Assert.Equal("Individual", dto.TipoParticipacion);
        }

        [Fact]
        public async Task ObtenerParticipantesDeUnaCuentaAsync_SinDatos_RetornaVacio()
        {
            using var ctx = CreateInMemoryContextWithParticipaciones();
            var repo = new CuentaRepositorio(ctx);

            var dtos = await repo.ObtenerParticipantesDeUnaCuentaAsync(1234);
            Assert.Empty(dtos);
        }

        private HangoContext CreateInMemoryContextWithParticipaciones(params ParticipacionCuenta[] items)
        {
            var opts = new DbContextOptionsBuilder<HangoContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new HangoContext(opts);
            ctx.ParticipacionCuenta.AddRange(items);
            ctx.SaveChanges();
            return ctx;
        }
    }
}
