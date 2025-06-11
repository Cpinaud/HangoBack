using Hango.Datos.EF;
using Hango.Logica.Login;
using Hango.Logica.Registro;
using Hango.Logica.Usuario;
using Hango.Logica.Utilidades;
using Hango.Repositorios.Avatar;
using Hango.Repositorios.RefreshToken;
using Hango.Repositorios.Usuario;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hango.Tests
{
    public class RegistroLoginTests
    {
 /*       private HangoContext GetInMemoryDb()
        {
            var dbName = Guid.NewGuid().ToString(); //le genera un nombre de base de datos unico para cada test
            var options = new DbContextOptionsBuilder <HangoContext> ()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new HangoContext(options);
        }

        [Fact]
        public async Task RegistrarUsuario_DeberiaRegistrarUsuarioExitosamente()
        {
            // Arrange

            var mockCorreoLogica = new Mock<ICorreoLogica>();
            mockCorreoLogica.Setup(m => m.EnviarCodigo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                            .Returns(Task.CompletedTask);
            var mockUsuarioRepositorio = new Mock<IUsuarioRepositorio>();
            mockUsuarioRepositorio.Setup(r => r.ExisteMail(It.IsAny<string>()))
                                    .ReturnsAsync(false);
            mockUsuarioRepositorio.Setup(r => r.AgregarUsuarioAsync(It.IsAny<Usuario>()))
                      .Returns(Task.FromResult(true));
            var registroLogica = new RegistroLogica(mockCorreoLogica.Object, mockUsuarioRepositorio.Object);

            var nuevoUsuario = new Usuario
            {
                Nombre = "TestUser",
                EstaVerificado = true,
                Email = "testuser@email.com",
                Password_Hash = PasswordHash.Hash("123456"),
                IdAvatar = 1,
                CreateAt = System.DateTime.Now,
                UpdateAt = System.DateTime.Now,
                CodigoVerificacion = System.Guid.NewGuid().ToString()
            };


            // Act
            var resultado = await registroLogica.RegistrarUsuario(nuevoUsuario);
            mockUsuarioRepositorio.Setup(r => r.ExisteMail(It.IsAny<string>()))
                                    .ReturnsAsync(true);
            // Assert
            Assert.True(resultado);
            mockUsuarioRepositorio.Verify(r => r.AgregarUsuarioAsync(It.Is<Usuario>(u => u.Email == nuevoUsuario.Email)), Times.Once);
            var usuarioExiste = await registroLogica.ExisteMail(nuevoUsuario.Email);
            Assert.True(usuarioExiste);

        }

        [Fact]
        public async Task Login_DeberiaDevolverUsuarioConCredencialesCorrectas()
        {
            // Arrange

            var mockCorreoLogica = new Mock<ICorreoLogica>();
            var mockUsuarioLogica = new Mock<IUsuarioLogica>();
            var mockRefreshTokenRepositorio = new Mock<IRefreshTokenRepositorio>();

            // IRefreshTokenRepositorio refreshTokenRepository, IUsuarioLogica usuarioLogica

            var loginLogica = new LoginLogica(mockRefreshTokenRepositorio.Object, mockUsuarioLogica.Object);


            var password = "contraseña123";
            var hashPassword = PasswordHash.Hash(password);

            var usuario = new Usuario
            {
                Nombre = "TestUser",
                EstaVerificado = true,
                Email = "loginuser@email.com",
                IdAvatar = 1,
                Password_Hash = hashPassword,
                CreateAt = System.DateTime.Now,
                UpdateAt = System.DateTime.Now,
                CodigoVerificacion = System.Guid.NewGuid().ToString()
            };

            mockUsuarioLogica.Setup(repo => repo.FindUserByEmailAndPass(usuario.Email, hashPassword))
            .ReturnsAsync(usuario);

            // Act
            var usuarioLogin = await loginLogica.ObtenerUserYContrasenia(usuario.Email, hashPassword);

            // Assert
            Assert.NotNull(usuarioLogin);
            Assert.Equal(usuario.Email, usuarioLogin.Email);
            Assert.Equal(usuario.Password_Hash, usuarioLogin.Password_Hash);
        }



        [Fact]
        public async Task Login_DeberiaDevolverNullSiContrasenaEsIncorrecta()
        {
            // Arrange
            var mockUsuarioRepositorio = new Mock<IUsuarioRepositorio>();
            var mockUsuarioLogica = new Mock<IUsuarioLogica>();
            var mockRefreshTokenRepositorio = new Mock<IRefreshTokenRepositorio>();

            var loginLogica = new LoginLogica(mockRefreshTokenRepositorio.Object, mockUsuarioLogica.Object);

            var usuario = new Usuario
            {
                Nombre = "TestUser",
                EstaVerificado = true,
                Email = "loginuser@email.com",
                Password_Hash = PasswordHash.Hash("correctPassword"),
                CreateAt = System.DateTime.Now,
                UpdateAt = System.DateTime.Now,
                CodigoVerificacion = System.Guid.NewGuid().ToString()
            };

            var hashPassword = PasswordHash.Hash("correctPassword");

            mockUsuarioRepositorio.Setup(repo => repo.FindUserByPassAndEmail(usuario.Email, hashPassword))
           .ReturnsAsync(usuario);

            // Act
            var usuarioLogin = await loginLogica.ObtenerUserYContrasenia(usuario.Email, PasswordHash.Hash("wrongPassword"));

            // Assert
            Assert.Null(usuarioLogin);
        }

        [Fact]
        public async Task RegistrarUsuario_DeberiaFallarSiEmailYaExiste()
        {
            // Arrange
            var mockCorreoLogica = new Mock<ICorreoLogica>();
            mockCorreoLogica.Setup(m => m.EnviarCodigo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                            .Returns(Task.CompletedTask);
            var mockUsuarioRepositorio = new Mock<IUsuarioRepositorio>();

            var registroLogica = new RegistroLogica(mockCorreoLogica.Object, mockUsuarioRepositorio.Object); ;

            var email = "duplicado@email.com";

            var usuarioExistente = new Usuario
            {
                Nombre = "Existente",
                Email = email,
                EstaVerificado = true,
                Password_Hash = PasswordHash.Hash("abc123"),
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                CodigoVerificacion = Guid.NewGuid().ToString()
            };

            var nuevoUsuario = new Usuario
            {
                Nombre = "Nuevo",
                Email = email,
                EstaVerificado = true,
                Password_Hash = PasswordHash.Hash("otra123"),
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                CodigoVerificacion = Guid.NewGuid().ToString()
            };


            var registro = await registroLogica.RegistrarUsuario(nuevoUsuario);

            // Act
            var resultado = await registroLogica.RegistrarUsuario(nuevoUsuario);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task Login_DeberiaDevolverNullSiEmailNoExiste()
        {
            // Arrange
            var mockUsuarioLogica = new Mock<IUsuarioLogica>();
            var mockRefreshTokenRepositorio = new Mock<IRefreshTokenRepositorio>();
            var loginLogica = new LoginLogica(mockRefreshTokenRepositorio.Object, mockUsuarioLogica.Object);

            var emailInexistente = "noexiste@email.com";
            var passwordHash = PasswordHash.Hash("cualquier");

            // Act
            var resultado = await loginLogica.ObtenerUserYContrasenia(emailInexistente, passwordHash);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public async Task RegistrarUsuario_DeberiaFallarSiFaltanDatos()
        {
            // Arrange
            var mockCorreoLogica = new Mock<ICorreoLogica>();
            var mockUsuarioRepositorio = new Mock<IUsuarioRepositorio>();

            var registroLogica = new RegistroLogica(mockCorreoLogica.Object, mockUsuarioRepositorio.Object);

            var usuario = new Usuario
            {
                Nombre = "", // nombre vacío
                Email = "faltandodato@email.com",
                EstaVerificado = true,
                Password_Hash = PasswordHash.Hash("123456"),
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                CodigoVerificacion = Guid.NewGuid().ToString()
            };

            // Act
            var resultado = await registroLogica.RegistrarUsuario(usuario);

            // Assert
            Assert.False(resultado);
        }
                */
    }
}
