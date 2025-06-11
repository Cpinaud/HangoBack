using Hango.Datos.EF;
using Hango.Repositorios.Presupuesto;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Tests.Repositorio.Presupuesto
{
    public class PresupuestoRepositorioTest
    {
        private readonly Mock<HangoContext> _mockContext;
        private readonly Mock<DbSet<Datos.EF.Presupuesto>> _mockDbSetPresupuesto;
        private readonly PresupuestoRepositorio _repositorio;

        public PresupuestoRepositorioTest()
        {
            _mockContext = new Mock<HangoContext>();
            _mockDbSetPresupuesto = new Mock<DbSet<Datos.EF.Presupuesto>>();

            _mockContext.Setup(c => c.Presupuesto)
                .Returns(_mockDbSetPresupuesto.Object);

            _repositorio = new PresupuestoRepositorio(_mockContext.Object);
        }

        [Fact]
    public async Task GuardarPresupuestoAsync_GuardaCorrectamente()
    {
        var p = new Datos.EF.Presupuesto { IdPresupuesto = 1, IdUsuario = 5, Rango = 10 };
        _mockDbSetPresupuesto
            .Setup(d => d.AddAsync(p, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Datos.EF.Presupuesto>)null);
        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _repositorio.GuardarPresupuestoAsync(p);

        _mockDbSetPresupuesto.Verify(d => d.AddAsync(p, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GuardarPresupuestoAsync_AddAsyncFallaLanzaExcepcion()
    {
        var p = new Datos.EF.Presupuesto();
        _mockDbSetPresupuesto
            .Setup(d => d.AddAsync(It.IsAny<Datos.EF.Presupuesto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Error AddAsync"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _repositorio.GuardarPresupuestoAsync(p));
    }

    [Fact]
    public async Task GuardarPresupuestoAsync_SaveFallaLanzaExcepcion()
    {
        var p = new Datos.EF.Presupuesto();
        _mockDbSetPresupuesto
            .Setup(d => d.AddAsync(It.IsAny<Datos.EF.Presupuesto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Datos.EF.Presupuesto>)null);
        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Error SaveChanges"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _repositorio.GuardarPresupuestoAsync(p));
    }
    [Fact]
    public async Task ActualizarPresupuestoAsync_ConPresupuestoExistente_ActualizaRango()
        {
            var usuario = new Datos.EF.Usuario
            {
                IdUsuario = 2,
                Presupuesto = new System.Collections.Generic.List<Datos.EF.Presupuesto>
            {
                new Datos.EF.Presupuesto { IdPresupuesto = 10, IdUsuario = 2, Rango = 5 }
            }
            };

            await _repositorio.ActualizarPresupuestoAsync(usuario, 20);

            Assert.Single(usuario.Presupuesto);
            Assert.Equal(20, usuario.Presupuesto.First().Rango);
        }

        [Fact]
        public async Task ActualizarPresupuestoAsync_SinPresupuesto_AgregaUnoNuevo()
        {
            var usuario = new Datos.EF.Usuario
            {
                IdUsuario = 3,
                Presupuesto = new System.Collections.Generic.List<Datos.EF.Presupuesto>()
            };

            await _repositorio.ActualizarPresupuestoAsync(usuario, 15);

            Assert.Single(usuario.Presupuesto);
            var p = usuario.Presupuesto.First();
            Assert.Equal(3, p.IdUsuario);
            Assert.Equal(15, p.Rango);
        }

        [Fact]
        public async Task ActualizarPresupuestoAsync_RangoNull_LanzaInvalidOperationException()
        {
            var usuario = new Datos.EF.Usuario
            {
                IdUsuario = 4,
                Presupuesto = new System.Collections.Generic.List<Datos.EF.Presupuesto>()
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _repositorio.ActualizarPresupuestoAsync(usuario, null));
        }

    }
}
