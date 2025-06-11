using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Hango.Controllers.Grupos;
using Hango.Logica.Grupos;
using Hango.Dispatcher;
using Hango.Logica.Usuario;
using Hango.Controllers.Grupos;
using Hango.Datos.DTO.Grupo;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Hango.Datos.DTO.Usuario;
using Hango.Datos.EF;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.RegularExpressions;
using Hango.Datos.DTO.Integrantes;

namespace Hango.Tests.Controladores
{
    public class GrupoControllerTest
    {
        private readonly Mock<IGrupoLogica> _mockGrupoLogica;
        private readonly Mock<ISignalRDispatcher> _mockSignalRDispatcher;
        private readonly Mock<IUsuarioLogica> _mockUsuarioLogica;
        private readonly GrupoController _controller;
        private readonly FormFile _mockArchivo;

        public GrupoControllerTest()
        {
            _mockGrupoLogica = new Mock<IGrupoLogica>();
            _mockSignalRDispatcher = new Mock<ISignalRDispatcher>();
            _mockUsuarioLogica = new Mock<IUsuarioLogica>();
            _controller = new GrupoController(_mockGrupoLogica.Object, _mockSignalRDispatcher.Object,_mockUsuarioLogica.Object);
            var contenidoFalso = "contenido de imagen";
            var bytes = Encoding.UTF8.GetBytes(contenidoFalso);
            var stream = new MemoryStream(bytes);

            _mockArchivo = new FormFile(stream, 0, bytes.Length, "Imagen", "foto.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

        private ControllerContext MockHttpContextConUsuario(int idUsuario)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString())
            }, "TestAuth");

            var principal = new ClaimsPrincipal(identity);

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }


        [Fact]
        public async Task ActualizarGrupo_SiTodoOk_EmiteEventoDespuesDeActualizar()
        {
            // Arrange
            var grupoDto = new GrupoUpdateDTO { Nombre = "GrupoTest" };
            var grupoActualizado = new GrupoDTO { IdGrupo = 1, Nombre = "GrupoTest" };
            var idUsuario = 1;
            _mockGrupoLogica.Setup(s => s.ActualizarGrupo(1, grupoDto, idUsuario))
                        .ReturnsAsync(grupoActualizado);
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);

            // Act
            await _controller.ActualizarGrupo(1, grupoDto);

            // Assert
            _mockSignalRDispatcher.Verify(d => d.CrearYEmitirHub(1, "MENSAJE", It.IsAny<string>(), idUsuario), Times.Once);
        }

        [Fact]
        public async Task ActualizarGrupo_SiAlgoFalla_DebeDevolverBadRequest()
        {
            // Arrange
            var grupoDto = new GrupoUpdateDTO { Nombre = "GrupoTest" };
            var grupoActualizado = new GrupoDTO { IdGrupo = 1, Nombre = "GrupoTest" };
            var idUsuario = 1;
            _mockGrupoLogica.Setup(s => s.ActualizarGrupo(1, grupoDto, idUsuario))
                        .ThrowsAsync(new Exception("Excepcion"));
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);

            // Act
            var resultado = await _controller.ActualizarGrupo(1, grupoDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
         public async Task ActualizarGrupo_SiTodoEstaCorrecto_DebeDevolverOkConGrupoDTO()
         {
            // Arrange
            var grupoDto = new GrupoUpdateDTO { Nombre = "GrupoTest" };
            var grupoActualizado = new GrupoDTO { IdGrupo = 1, Nombre = "GrupoTest" };
            var idUsuario = 1;
            _mockGrupoLogica.Setup(s => s.ActualizarGrupo(1, grupoDto, idUsuario))
                        .ReturnsAsync(grupoActualizado);
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);

            // Act
            var resultado = await _controller.ActualizarGrupo(1, grupoDto);

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            var datos = Assert.IsType<GrupoDTO>(resultOk.Value);
            Assert.Equal("GrupoTest", datos.Nombre);
        }


        [Fact]
        public async Task InvitacionAUnGrupo_SiGrupoEsNull_DebeDevolverNotFound()
        {
            // Arrange
            var token = "0de2a205-f268-486a-8b32-96dc348403a3";
            _mockGrupoLogica.Setup(s => s.ObtenerGrupoPorTokenInvitacion(token))
                        .ReturnsAsync(null as GrupoShortDTO);

            // Act
            var resultado = await _controller.InvitacionAUnGrupo(token);

            // Assert
            Assert.IsType<NotFoundObjectResult>(resultado);
        }


        [Fact]
        public async Task InvitacionAUnGrupo_SiTodoEstaCorrecto_DebeDevolverOkConGrupoShortDTO()
        {
            // Arrange
            var token = "0de2a205-f268-486a-8b32-96dc348403a3";
            var grupo = new GrupoShortDTO { IdGrupo = 1, Nombre = "GrupoTest" };
            _mockGrupoLogica.Setup(s => s.ObtenerGrupoPorTokenInvitacion(token))
                        .ReturnsAsync(grupo);

            // Act
            var resultado = await _controller.InvitacionAUnGrupo(token);

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            Assert.IsType<GrupoShortDTO>(resultOk.Value);
        }

        [Fact]
        public async Task UnirseAlGrupoMedianteLink_SiGrupoEsNull_DebeDevolverNotFound()
        {
            // Arrange
            var token = "0de2a205-f268-486a-8b32-96dc348403a3";
            _mockGrupoLogica.Setup(s => s.ObtenerGrupoPorTokenInvitacion(token))
                        .ReturnsAsync(null as GrupoShortDTO);
            // Act
            var resultado = await _controller.UnirseAlGrupoMedianteLink(token);

            // Assert
            Assert.IsType<NotFoundObjectResult>(resultado);
        }


        [Fact]
        public async Task UnirseAlGrupoMedianteLink_SiTokoOk_DebeDevolverOkConGrupo()
        {
            // Arrange
            var token = "0de2a205-f268-486a-8b32-96dc348403a3";
            var grupo = new GrupoShortDTO { IdGrupo = 1, Nombre = "GrupoTest" };
            var usuario = new PerfilUsuarioDTO { IdUsuario = 1, Nombre = "UsuarioTest" };
            var idUsuario = 1;
            _mockUsuarioLogica.Setup(s => s.ObtenerPerfilUsuarioById(idUsuario))
                        .ReturnsAsync(usuario);
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerGrupoPorTokenInvitacion(token))
                        .ReturnsAsync(grupo);
            _mockGrupoLogica.Setup(s => s.UnirseAlGrupo(idUsuario, grupo.IdGrupo))
                        .ReturnsAsync(true);

            // Act
            var resultado = await _controller.UnirseAlGrupoMedianteLink(token);

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            var grupoResultado = Assert.IsType<GrupoShortDTO>(resultOk.Value);
            Assert.Equal(grupo.IdGrupo, grupoResultado.IdGrupo);
        }

        [Fact]
        public async Task UnirseAlGrupoMedianteLink_SiTokoOk_EmiteEventoDespuesDeUnirse()
        {
            // Arrange
            var token = "0de2a205-f268-486a-8b32-96dc348403a3";
            var grupo = new GrupoShortDTO { IdGrupo = 1, Nombre = "GrupoTest" };
            var usuario = new PerfilUsuarioDTO { IdUsuario = 1, Nombre = "UsuarioTest" };
            var idUsuario = 1;
            _mockUsuarioLogica.Setup(s => s.ObtenerPerfilUsuarioById(idUsuario))
                        .ReturnsAsync(usuario);
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerGrupoPorTokenInvitacion(token))
                        .ReturnsAsync(grupo);
            _mockGrupoLogica.Setup(s => s.UnirseAlGrupo(idUsuario, grupo.IdGrupo))
                        .ReturnsAsync(true);

            // Act
            var resultado = await _controller.UnirseAlGrupoMedianteLink(token);

            // Assert
            _mockSignalRDispatcher.Verify(d => d.CrearYEmitirHub(1, "MENSAJE", It.IsAny<string>(), idUsuario), Times.Once);
        }


        [Fact]
        public async Task UnirseAlGrupoMedianteLink_SiAlgoFalla_DebeDevolverBadRequest()
        {
            // Arrange
            var token = "0de2a205-f268-486a-8b32-96dc348403a3";
            var grupo = new GrupoShortDTO { IdGrupo = 1, Nombre = "GrupoTest" };
            var idUsuario = 1;
            var usuario = new PerfilUsuarioDTO { IdUsuario = 1, Nombre = "UsuarioTest" };
            _mockUsuarioLogica.Setup(s => s.ObtenerPerfilUsuarioById(idUsuario))
                        .ReturnsAsync(usuario);
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerGrupoPorTokenInvitacion(token))
                        .ReturnsAsync(grupo);
            _mockGrupoLogica.Setup(s => s.UnirseAlGrupo(idUsuario, grupo.IdGrupo))
                        .ThrowsAsync(new Exception("Excepcion"));

            // Act
            var resultado = await _controller.UnirseAlGrupoMedianteLink(token);

            // Assert
             Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task RegenerarLinkInvitacion_SiTodoOk_DebeDevolverOkConToken()
        {
            // Arrange
            var idGrupo = 1;
            var idUsuario = 1;
            _mockGrupoLogica.Setup(s => s.RegenerarTokenInvitacionGrupo(idGrupo, idUsuario))
                        .ReturnsAsync("nuevo-token-invitacion");
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);


            // Act
            var resultado = await _controller.RegenerarLinkInvitacion(idGrupo);

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            Assert.IsType<string>(resultOk.Value);
            Assert.Equal("nuevo-token-invitacion", resultOk.Value);
        }


        [Fact]
        public async Task RegenerarLinkInvitacion_SiAlgoFalla_DebeDevolverBadRequest()
        {
            // Arrange
            var idGrupo = 1;
            var idUsuario = 1;
            _mockGrupoLogica.Setup(s => s.RegenerarTokenInvitacionGrupo(idGrupo,idUsuario))
                        .ThrowsAsync(new Exception("Excepcion"));
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            

            // Act
            var resultado = await _controller.RegenerarLinkInvitacion(idGrupo);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task CrearGrupo_SiTodoOk_DebeDevolverOkConIdGrupo()
        {
            // Arrange
            var idUsuario = 1;
            var grupo = new GrupoInsertDTO
            {
                Nombre = "GrupoTest",
                Zonas = new List<string> { "Zona1", "Zona2" },
                Imagen = _mockArchivo
            };
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.CrearGrupo(grupo, idUsuario))
                        .ReturnsAsync(new Grupo
                        {
                            IdGrupo = 1,
                            Nombre = "GrupoTest",
                            UrlImagen = "url-imagen-test",
                            Activo = false,
                            TokenInvitacion= "token-invitacion-test",

                        });
            // Act
            var resultado = await _controller.CrearGrupo(grupo);

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal(1, resultOk.Value);
            Assert.IsType<int>(resultOk.Value);
        }

        [Fact]
        public async Task CrearGrupo_SiAlgoFalla_DebeDevolverBadRequest()
        {
            // Arrange
            var idUsuario = 1;
            var grupo = new GrupoInsertDTO
            {
                Nombre = null,
                Zonas = new List<string> { "Zona1", "Zona2" },
                Imagen = _mockArchivo
            };
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.CrearGrupo(grupo, idUsuario))
                        .ThrowsAsync(new Exception("Excepcion"));
            // Act
            var resultado = await _controller.CrearGrupo(grupo);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task CrearGrupo_SiModeloIncorrecto_DebeDevolverBadRequestConMensajeDeErrorModel()
        {
            // Arrange
            var grupo = new GrupoInsertDTO
            {
                Nombre = null,
                Zonas = new List<string> { "Zona1", "Zona2" },
            };
            _controller.ModelState.AddModelError("Nombre", "El nombre es obligatorio");

            // Act
            var resultado = await _controller.CrearGrupo(grupo);

            // Assert
            var resultFail = Assert.IsType<BadRequestObjectResult>(resultado);
            var errores = Assert.IsType<SerializableError>(resultFail.Value);
            Assert.True(errores.ContainsKey("Nombre"));

            var erroresDelNombre = errores["Nombre"] as string[];
            Assert.NotNull(erroresDelNombre);
            Assert.Contains("El nombre es obligatorio", erroresDelNombre);
        }

        [Fact]
        public async Task ObtenerGrupoPorId_SiTokoOk_DevuelveOkConGrupo()
        {
            // Arrange
            var idUsuario = 1;
            var idGrupo = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerGrupoPorId(idGrupo, idUsuario))
                        .ReturnsAsync(new GrupoDTO
                        {
                            IdGrupo = idGrupo,
                            Nombre = "GrupoTest",
                            UrlImagen = "url-imagen-test",
                            Activo = true,
                            TokenInvitacion = "token-invitacion-test"
                        });
            // Act
            var resultado = await _controller.ObtenerGrupoPorId(idGrupo);

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            var grupo = Assert.IsType<GrupoDTO>(resultOk.Value);
            Assert.Equal(idGrupo, grupo.IdGrupo);
        }


        [Fact]
        public async Task ObtenerGrupoPorId_SiAlgoFalla_DevuelveBadRequest() {
            // Arrange
         var idUsuario = 1;
         var idGrupo = 1;
         _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
         _mockGrupoLogica.Setup(s => s.ObtenerGrupoPorId(idGrupo, idUsuario))
                     .ThrowsAsync(new Exception("Excepcion"));
         // Act
         var resultado = await _controller.ObtenerGrupoPorId(idGrupo);

         // Assert
         Assert.IsType<BadRequestObjectResult>(resultado);
         }

        [Fact]
        public async Task ObtenerIntegrantesDeGrupo_SiTodoOk_DevuelveOkConIntegrantes()
        {
            // Arrange
            var idUsuario = 1;
            var idGrupo = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerIntegrantesGrupo(idGrupo, idUsuario))
                        .ReturnsAsync(new List<IntegranteDTO>
                        {
                            new IntegranteDTO { IdUsuario = 1, Nombre = "Usuario1",Administrador=true },
                            new IntegranteDTO { IdUsuario = 2, Nombre = "Usuario2",Administrador=false  }
                        });
            // Act
            var resultado = await _controller.ObtenerIntegrantesDeGrupo(idGrupo);

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            Assert.IsType<List<IntegranteDTO>>(resultOk.Value);
        }

        [Fact]
        public async Task ObtenerIntegrantesDeGrupo_SiAlgoFalla_DevuelveBadRequest()
        {
            // Arrange
            var idUsuario = 1;
            var idGrupo = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerIntegrantesGrupo(idGrupo, idUsuario))
                        .ThrowsAsync(new Exception("Excepcion"));
            // Act
            var resultado = await _controller.ObtenerIntegrantesDeGrupo(idGrupo);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }


        [Fact]
        public async Task EliminarIntregranteGrupo_SiTodoOk_EmiteEventoDespuesDeEliminar()
        {
            // Arrange
            var idUsuario = 1;
            var idUsuarioEliminador = 1;
            var idGrupo = 1;
            var usuario = new PerfilUsuarioDTO { IdUsuario = 1, Nombre = "UsuarioTest" };
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.EliminarIntegranteGrupo(idGrupo, idUsuario, idUsuarioEliminador))
                        .Returns(Task.CompletedTask);
            _mockUsuarioLogica.Setup(s => s.ObtenerPerfilUsuarioById(idUsuario))
                        .ReturnsAsync(usuario);
            // Act
            var resultado = await _controller.EliminarIntregranteGrupo(idGrupo, idUsuario);

            // Assert
            _mockSignalRDispatcher.Verify(d => d.CrearYEmitirHub(1, "MENSAJE", It.IsAny<string>(), idUsuario), Times.Once);
        }

        [Fact]
        public async Task EliminarIntregranteGrupo_SiTodoOk_DevuelveOk()
        {
            // Arrange
            var idUsuario = 1;
            var idUsuarioEliminador = 1;
            var idGrupo = 1;
            var usuario = new PerfilUsuarioDTO { IdUsuario = 1, Nombre = "UsuarioTest" };
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.EliminarIntegranteGrupo(idGrupo, idUsuario, idUsuarioEliminador))
                        .Returns(Task.CompletedTask);
            _mockUsuarioLogica.Setup(s => s.ObtenerPerfilUsuarioById(idUsuario))
                        .ReturnsAsync(usuario);
            // Act
            var resultado = await _controller.EliminarIntregranteGrupo(idGrupo, idUsuario);

            // Assert
            Assert.IsType<OkResult>(resultado);
        }


        [Fact]
        public async Task EliminarIntregranteGrupo_SiAlgoFalla_DevuelveBadRequest()
        {
            // Arrange
            var idUsuario = 1;
            var idUsuarioEliminador = 1;
            var idGrupo = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.EliminarIntegranteGrupo(idGrupo, idUsuario, idUsuarioEliminador))
                        .ThrowsAsync(new Exception("Excepcion"));
            // Act
            var resultado = await _controller.EliminarIntregranteGrupo(idGrupo, idUsuario);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task ObtenerGruposDelUsuario_SiTodoOk_DevuelveOkConGrupos()
        {
            // Arrange
            var idUsuario = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerGruposDelUsuario(idUsuario))
                        .ReturnsAsync(new List<GrupoShortDTO>
                        {
                            new GrupoShortDTO { IdGrupo = 1, Nombre = "Grupo1" },
                            new GrupoShortDTO { IdGrupo = 2, Nombre = "Grupo2" }
                        });
            // Act
            var resultado = await _controller.ObtenerGruposDelUsuario();

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            Assert.IsType<List<GrupoShortDTO>>(resultOk.Value);
            var grupos = resultOk.Value as List<GrupoShortDTO>;
            Assert.NotNull(grupos);

        }


        [Fact]
        public async Task ObtenerGruposDelUsuario_SiAlgoFalla_DevuelveBadRequest()
        {
            // Arrange
            var idUsuario = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerGruposDelUsuario(idUsuario))
                        .ThrowsAsync(new Exception("Excepcion"));
            // Act
            var resultado = await _controller.ObtenerGruposDelUsuario();

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

     

        [Fact]
        public async Task ObtenerPlanesConfirmadosDelGrupo_SiTokoOk_DevuelveOkConPlanes()
        {
            // Arrange
            var idUsuario = 1;
            var idGrupo = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerPlanesConfirmadosDelGrupo(idGrupo, idUsuario))
                        .ReturnsAsync(new List<Planes>
                        {
                            new Planes { Id = 1, PropuestaId = 1, DiaSemana=2,Fecha= DateOnly.FromDateTime(DateTime.Now), PreferenciaId=1, Hora=TimeSpan.FromHours(18), Lugar="Lugar1", Descripcion="Descripcion1", Direccion="Direccion1", Estado="Confirmado", Presupuesto="$100" },
                            new Planes { Id = 2, PropuestaId = 1, DiaSemana=2,Fecha= DateOnly.FromDateTime(DateTime.Now), PreferenciaId=1, Hora=TimeSpan.FromHours(18), Lugar="Lugar1", Descripcion="Descripcion1", Direccion="Direccion1", Estado="Confirmado", Presupuesto="$100" },
                        });
            // Act
            var resultado = await _controller.ObtenerPlanesConfirmadosDelGrupo(idGrupo);

            // Assert
            Assert.IsType<OkObjectResult>(resultado);
        }

        [Fact]
        public async Task ObtenerPlanesConfirmadosDelGrupo_SiAlgoFalla_DevuelveBadRequest()
        {
            // Arrange
            var idUsuario = 1;
            var idGrupo = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ObtenerPlanesConfirmadosDelGrupo(idGrupo,idUsuario))
                        .ThrowsAsync(new Exception("Excepcion"));
            // Act
            var resultado = await _controller.ObtenerPlanesConfirmadosDelGrupo(idGrupo);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task ActivarGrupo_SiTodoOk_DevuelveOkConMensaje()
        {
            // Arrange
            var idUsuario = 1;
            var idGrupo = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ActivarGrupo(idGrupo, idUsuario))
                        .Returns(Task.CompletedTask);
            // Act
            var resultado = await _controller.ActivarGrupo(idGrupo);

            // Assert
            var resultOk = Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("Grupo activado.", resultOk.Value);
            Assert.IsType<string>(resultOk.Value);
        }

        [Fact]
        public async Task ActivarGrupo_SiAlgoFalla_DevuelveBadRequest()
        {
            // Arrange
            var idUsuario = 1;
            var idGrupo = 1;
            _controller.ControllerContext = MockHttpContextConUsuario(idUsuario);
            _mockGrupoLogica.Setup(s => s.ActivarGrupo(idGrupo, idUsuario))
                        .ThrowsAsync(new Exception("Excepcion"));
            // Act
            var resultado = await _controller.ActivarGrupo(idGrupo);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

    }
}
