using Hango.Datos.EF;
using Hango.Repositorios.Usuario;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Tests.Repositorio.Usuario
{
    public class UsuarioRepositorioTest
    {
        private readonly Mock<HangoContext> _mockContext;
        private readonly Mock<DbSet<Datos.EF.Usuario>> _mockSetUsuario;
        private readonly UsuarioRepositorio _repo;

        public UsuarioRepositorioTest()
        {
            _mockContext = new Mock<HangoContext>();
            _mockSetUsuario = new Mock<DbSet<Datos.EF.Usuario>>();
            _mockContext.Setup(c => c.Usuario).Returns(_mockSetUsuario.Object);
            _repo = new UsuarioRepositorio(_mockContext.Object);
        }
        [Fact]
        public async Task ActualizarUsuarioAsync_RetornaTrueYGuardaElUsuario()
        {
            var user = new Datos.EF.Usuario { IdUsuario = 1 };
            _mockSetUsuario.Setup(d => d.Update(user));
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _repo.ActualizarUsuarioAsync(user);

            Assert.True(result);
            _mockSetUsuario.Verify(d => d.Update(user), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ActualizarUsuarioAsync_IntentaGuardarFallaLanzaException()
        {
            var user = new Datos.EF.Usuario();
            _mockSetUsuario.Setup(d => d.Update(It.IsAny<Datos.EF.Usuario>()));
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("fail"));

            await Assert.ThrowsAsync<Exception>(() => _repo.ActualizarUsuarioAsync(user));
        }

        [Fact]
        public async Task AgregarUsuarioAsync_RetornaTrueYAgregaUsuario()
        {
            var user = new Datos.EF.Usuario { IdUsuario = 2 };
            _mockSetUsuario.Setup(d => d.AddAsync(user, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Datos.EF.Usuario>)null);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var res = await _repo.AgregarUsuarioAsync(user);
            Assert.True(res);
        }

        [Fact]
        public async Task AgregarUsuarioAsync_IntentaAgregarFallaYLanzaException()
        {
            _mockSetUsuario.Setup(d => d.AddAsync(It.IsAny<Datos.EF.Usuario>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException());
            await Assert.ThrowsAsync<Exception>(() => _repo.AgregarUsuarioAsync(new Datos.EF.Usuario()));
        }

   
    /*    [Fact]
        public async Task ExisteMail_CuandoExisteRetornaTrue()
        {
            using var ctx = CreateInMemory(new Datos.EF.Usuario { Email = "prueba@email.com" });
            var repoMem = new UsuarioRepositorio(ctx);
            Assert.True(await repoMem.ExisteMail("prueba@email.com"));
        } */
        [Fact]
        public async Task ExisteMail_CuandoNoExisteRetornaFalse()
        {
            using var ctx = CreateInMemory();
            var repoMem = new UsuarioRepositorio(ctx);
            Assert.False(await repoMem.ExisteMail("prueba@email.com"));
        }


/*        [Fact]
        public async Task FindUserByCodeVerificacion_ExisteDevuelveUsuario()
        {
            using var ctx = CreateInMemory(new Datos.EF.Usuario { CodigoVerificacion = "abc" });
            var repoMem = new UsuarioRepositorio(ctx);
            var u = await repoMem.FindUserByCodeVerificacion("abc");
            Assert.Equal("abc", u.CodigoVerificacion);
        }
        [Fact]
        public async Task FindUserByCodeVerificacion_NoExisteLanzaException()
        {
            using var ctx = CreateInMemory();
            var repoMem = new UsuarioRepositorio(ctx);
            var ex = await Assert.ThrowsAsync<Exception>(() => repoMem.FindUserByCodeVerificacion("zzz"));
            Assert.Equal("Usuario no encontrado", ex.InnerException.Message);
        }
        [Fact]
        public async Task FindUserById_Existe_DevuelveUsuario()
        {
            using var ctx = CreateInMemory(new Datos.EF.Usuario { IdUsuario = 5 });
            var repoMem = new UsuarioRepositorio(ctx);

            var u = await repoMem.FindUserById(5);
            Assert.Equal(5, u.IdUsuario);
        }
        [Fact]
        public async Task FindUserById_NoExiste_LanzaException()
        {
            using var ctx = CreateInMemory();
            var repoMem = new UsuarioRepositorio(ctx);
            var ex = await Assert.ThrowsAsync<Exception>(() => repoMem.FindUserById(999));
            Assert.Equal("Usuario no encontrado", ex.InnerException.Message);
        }

        [Fact]
        public async Task FindUserByEmail_ExisteDevuelveUsuario()
        {
            using var ctx = CreateInMemory(new Datos.EF.Usuario { Email = "prueba@email.com" });
            var repoMem = new UsuarioRepositorio(ctx);
            var u = await repoMem.FindUserByEmail("prueba@email.com");
            Assert.Equal("prueba@email.com", u.Email);
        }
        [Fact]
        public async Task FindUserByEmail_NoExisteLanzaException()
        {
            using var ctx = CreateInMemory();
            var repoMem = new UsuarioRepositorio(ctx);
            var ex = await Assert.ThrowsAsync<Exception>(() => repoMem.FindUserByEmail("in@e.com"));
            Assert.Equal("Usuario no encontrado", ex.InnerException.Message);
        }

     
        [Fact]
        public async Task FindUserByPassAndEmail_ExisteYVerificadoDevuelveUsuario()
        {
            var user = new Datos.EF.Usuario { Email = "prueba@email.com", Password_Hash = "hash", EstaVerificado = true };
            using var ctx = CreateInMemory(user);
            var repoMem = new UsuarioRepositorio(ctx);
            var u = await repoMem.FindUserByPassAndEmail("prueba@email.com", "hash");
            Assert.Equal("e@e.com", u.Email);
        }
        [Fact]
        public async Task FindUserByPassAndEmail_NoExisteLanzaException()
        {
            using var ctx = CreateInMemory();
            var repoMem = new UsuarioRepositorio(ctx);
            var ex = await Assert.ThrowsAsync<Exception>(() => repoMem.FindUserByPassAndEmail("x", "prueba@email.com"));
            Assert.Equal("Usuario no encontrado", ex.InnerException.Message);
        }


        /*   [Fact]
                 public async Task ConfirmarCuenta_Valido_RetornaTrueYActualiza()
                 {
                     var user = new Datos.EF.Usuario { CodigoVerificacion = "cv", EstaVerificado = false };
                     using var ctx = CreateInMemory(user);
                     var repoMem = new UsuarioRepositorio(ctx);

                     var res = await repoMem.ConfirmarCuenta("cv");

                     Assert.True(res);
                     var u2 = ctx.Usuario.First();
                     Assert.True(u2.EstaVerificado);
                     Assert.Null(u2.CodigoVerificacion);
                     Assert.NotNull(u2.FechaVerificacion);
                 } */
        /*    [Fact]
            public async Task ConfirmarCuenta_Invalido_RetornaFalse()
            {
                var dbContextMock = new Mock<HangoContext>();
                var usuarioDbSetMock = new Mock<DbSet<Datos.EF.Usuario>>();
                dbContextMock.Setup(c => c.Usuario).Returns(usuarioDbSetMock.Object);
                usuarioDbSetMock.Setup(u => u.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Usuario, bool>>>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync((Usuario)null!);

                var repositorio = new UsuarioRepositorio(dbContextMock.Object);


                var ex = await Assert.ThrowsAsync<Exception>(() => repositorio.ConfirmarCuenta("codigo_invalido"));
                Assert.Equal("Usuario no encontrado", ex.Message);
            } */


   /*     [Fact]
        public async Task ObtenerUsuarioConRelacionesAsync_Existe_MapeaEntidades()
        {
            var usuario = new Datos.EF.Usuario { IdUsuario = 7 };
            usuario.HorarioDisponible = new List<HorarioDisponible> { new HorarioDisponible() };
            usuario.Usuario_Preferencia = new List<Usuario_Preferencia> { new Usuario_Preferencia { IdPreferenciaNavigation = new Datos.EF.Preferencia() } };
            usuario.Presupuesto = new List<Datos.EF.Presupuesto> { new Datos.EF.Presupuesto() };

            using var ctx = CreateInMemory(usuario);
         
            ctx.HorarioDisponible.AddRange(usuario.HorarioDisponible);
            ctx.Usuario_Preferencia.AddRange(usuario.Usuario_Preferencia);
            ctx.Preferencia.Add(usuario.Usuario_Preferencia.First().IdPreferenciaNavigation);
            ctx.Presupuesto.AddRange(usuario.Presupuesto);
            ctx.SaveChanges();

            var repoMem = new UsuarioRepositorio(ctx);

            var u = await repoMem.ObtenerUsuarioConRelacionesAsync(7);
            Assert.NotNull(u);
            Assert.NotEmpty(u.HorarioDisponible);
            Assert.NotEmpty(u.Usuario_Preferencia);
            Assert.NotEmpty(u.Presupuesto);
        } */
        [Fact]
        public async Task ObtenerUsuarioConRelacionesAsync_NoExiste_LanzaException()
        {
            using var ctx = CreateInMemory();
            var repoMem = new UsuarioRepositorio(ctx);
            await Assert.ThrowsAsync<Exception>(() => repoMem.ObtenerUsuarioConRelacionesAsync(999));
        }

      
        [Fact]
        public async Task GuardarCambiosAsync_CaminoFeliz_LlamaSaveChanges()
        {
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            await _repo.GuardarCambiosAsync();
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task GuardarCambiosAsync_Falla_LanzaException()
        {
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException());
            await Assert.ThrowsAsync<DbUpdateException>(() => _repo.GuardarCambiosAsync());
        }
        private HangoContext CreateInMemory(params Datos.EF.Usuario[] users)
        {
            var opts = new DbContextOptionsBuilder<HangoContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new HangoContext(opts);
            if (users?.Any() == true)
            {
                ctx.Usuario.AddRange(users);
                ctx.SaveChanges();
            }
            return ctx;
        }
    }
}
