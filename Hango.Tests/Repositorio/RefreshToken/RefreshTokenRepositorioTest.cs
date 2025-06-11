using Hango.Datos.EF;
using Hango.Repositorios.RefreshToken;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Tests.Repositorio.RefreshToken
{
    public class RefreshTokenRepositorioTest
    {
        private readonly Mock<HangoContext> _mockContext;
        private readonly Mock<DbSet<Datos.EF.RefreshToken>> _mockDbSet;
        private readonly RefreshTokenRepositorio _repositorio;

        public RefreshTokenRepositorioTest()
        {
            
            _mockContext = new Mock<HangoContext>();
            _mockDbSet = new Mock<DbSet<Datos.EF.RefreshToken>>();

            _mockContext
                .Setup(c => c.RefreshToken)
                .Returns(_mockDbSet.Object);

            _repositorio = new RefreshTokenRepositorio(_mockContext.Object);
        }

        [Fact]
        public async Task FindRefreshToken_ExisteDevuelveToken()
        {
            var token = new Datos.EF.RefreshToken { Id = 1, Token = "abc123" };
            using var ctx = CreateInMemoryContext(token);
            var repo = new RefreshTokenRepositorio(ctx);

            var result = await repo.FindRefreshToken("abc123");

            Assert.Equal(token.Id, result.Id);
            Assert.Equal("abc123", result.Token);
        }

        [Fact]
        public async Task FindRefreshToken_NoExiste_LanzaException()
        {
            using var ctx = CreateInMemoryContext();
            var repo = new RefreshTokenRepositorio(ctx);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                repo.FindRefreshToken("no-existe"));

            Assert.Equal("Refresh token no encontrado", ex.Message);
        }

        [Fact]
        public async Task GuardarRefreshTokenAsync_AgregaYGuarda()
        {
            var token = new Datos.EF.RefreshToken { Token = "token123" };
            _mockDbSet
                .Setup(d => d.AddAsync(token, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Datos.EF.RefreshToken>)null);
            _mockContext
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _repositorio.GuardarRefreshTokenAsync(token);

            _mockDbSet.Verify(d => d.AddAsync(token, It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GuardarRefreshTokenAsync_NoGuardaLanzaExcepcion()
        {
            var token = new Datos.EF.RefreshToken { Token = "fallido" };

            _mockDbSet
                .Setup(d => d.AddAsync(token, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("fail"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _repositorio.GuardarRefreshTokenAsync(token));

            Assert.Equal("Error al actualizar los datos en la base de datos.", ex.Message);
            Assert.IsType<DbUpdateException>(ex.InnerException);
        }

        [Fact]
        public async Task RevocarRefreshTokenAsync_CaminoFeliz_DevuelveTrueYMarcaRevocado()
        {
            var token = new Datos.EF.RefreshToken { Id = 2, Token = "t2", RevocarToken = null };
            using var ctx = CreateInMemoryContext(token);
            var repo = new RefreshTokenRepositorio(ctx);

            var result = await repo.RevocarRefreshTokenAsync("t2");

            Assert.True(result);
            var updated = ctx.RefreshToken.First();
            Assert.NotNull(updated.RevocarToken);
        }

        [Fact]
        public async Task RevocarRefreshTokenAsync_SiYaRevocado_DevuelveFalse()
        {
            var token = new Datos.EF.RefreshToken { Id = 3, Token = "t3", RevocarToken = DateTime.UtcNow.AddDays(-1) };
            using var ctx = CreateInMemoryContext(token);
            var repo = new RefreshTokenRepositorio(ctx);

            var result = await repo.RevocarRefreshTokenAsync("t3");

            Assert.False(result);
        }

     
        private HangoContext CreateInMemoryContext(params Datos.EF.RefreshToken[] tokens)
        {
            var options = new DbContextOptionsBuilder<HangoContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new HangoContext(options);
            if (tokens != null && tokens.Length > 0)
            {
                ctx.RefreshToken.AddRange(tokens);
                ctx.SaveChanges();
            }
            return ctx;
        }
    }
}
