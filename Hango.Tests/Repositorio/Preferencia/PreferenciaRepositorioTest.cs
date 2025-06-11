using Hango.Datos.DTO.Preferencia;
using Hango.Datos.EF;
using Hango.Repositorios.Preferencia;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hango.Tests.Repositorio.Preferencia
{
    public class PreferenciaRepositorioTest
    {
        private readonly Mock<HangoContext> _mockContext;
        private readonly Mock<DbSet<Datos.EF.Preferencia>> _mockDbSetPreferencia;
        private readonly Mock<DbSet<Usuario_Preferencia>> _mockDbSetUsuarioPreferencia;
        private readonly PreferenciaRepositorio _repositorio;

        public PreferenciaRepositorioTest()
        {
            _mockContext = new Mock<HangoContext>();
            _mockDbSetPreferencia = new Mock<DbSet<Datos.EF.Preferencia>>();
            _mockDbSetUsuarioPreferencia = new Mock<DbSet<Usuario_Preferencia>>();
            _mockContext
                .Setup(c => c.Preferencia)
                .Returns(_mockDbSetPreferencia.Object);

            _mockContext
                .Setup(c => c.Usuario_Preferencia)
                .Returns(_mockDbSetUsuarioPreferencia.Object);
            _repositorio = new PreferenciaRepositorio(_mockContext.Object);
        }

        [Fact]
        public async Task ObtenerPreferencias_ConDatosLosDevuelve()
        {
            using var ctx = CreateInMemoryContext(seed: c =>
            {
                c.Preferencia.Add(new Datos.EF.Preferencia { IdPreferencia = 1, Nombre = "Futbol" });
                c.Preferencia.Add(new Datos.EF.Preferencia { IdPreferencia = 2, Nombre = "Tenis" });
            });
            var repo = new PreferenciaRepositorio(ctx);

            var list = await repo.ObtenerPreferencias();

            Assert.Equal(2, list.Count);
            Assert.Contains(list, p => p.Nombre == "Futbol");
            Assert.Contains(list, p => p.Nombre == "Tenis");
        }

        [Fact]
        public async Task ObtenerPreferencias_RetornaVacio()
        {
            using var ctx = CreateInMemoryContext();
            var repo = new PreferenciaRepositorio(ctx);

            var list = await repo.ObtenerPreferencias();

            Assert.Empty(list);
        }

        [Fact]
        public async Task ObtenerPreferenciasPorUsuario_DevuelveLasPreferencias()
        {
            using var ctx = CreateInMemoryContext(seed: c =>
            {
                c.Preferencia.AddRange(
                    new Datos.EF.Preferencia { IdPreferencia = 1, Nombre = "Futbol" },
                    new Datos.EF.Preferencia { IdPreferencia = 2, Nombre = "Tenis" });
                c.Usuario_Preferencia.AddRange(
                    new Usuario_Preferencia { IdUsuario = 10, IdPreferencia = 1 },
                    new Usuario_Preferencia { IdUsuario = 10, IdPreferencia = 2 },
                    new Usuario_Preferencia { IdUsuario = 20, IdPreferencia = 1 });
            });
            var repo = new PreferenciaRepositorio(ctx);

            var list = await repo.ObtenerPreferenciasPorUsuario(10);

            Assert.Equal(2, list.Count);
            Assert.All(list, p => Assert.Contains(p.IdPreferencia, new[] { 1, 2 }));
        }

        [Fact]
        public async Task ObtenerPreferenciasPorUsuario_RetornaVacio()
        {
            using var ctx = CreateInMemoryContext();
            var repo = new PreferenciaRepositorio(ctx);

            var list = await repo.ObtenerPreferenciasPorUsuario(999);

            Assert.Empty(list);
        }
        [Fact]
        public async Task ObtenerPreferenciasGrupoAsync_ConRelaciones_LasDevuelve()
        {
            using var ctx = CreateInMemoryContext(seed: c =>
            {
                var p = new Datos.EF.Preferencia { IdPreferencia = 5, Nombre = "Futbol" };
                c.Preferencia.Add(p);
                c.Grupo_Preferencia.Add(new Grupo_Preferencia
                {
                    IdGrupo = 100,
                    IdPreferencia = 5,
                    IdPreferenciaNavigation = p
                });
            });
            var repo = new PreferenciaRepositorio(ctx);

            var list = await repo.ObtenerPreferenciasGrupoAsync(100);

            Assert.Single(list);
            Assert.Equal("Futbol", list[0].Nombre);
        }

        [Fact]
        public async Task ObtenerPreferenciasGrupoAsync_RetornaVacio()
        {
            using var ctx = CreateInMemoryContext();
            var repo = new PreferenciaRepositorio(ctx);

            var list = await repo.ObtenerPreferenciasGrupoAsync(1234);

            Assert.Empty(list);
        }

        [Fact]
        public async Task ObtenerIdPreferenciaPorNombre_ExisteRetornaIdPreferencia()
        {
            using var ctx = CreateInMemoryContext(seed: c =>
            {
                c.Preferencia.Add(new Datos.EF.Preferencia { IdPreferencia = 1, Nombre = "Futbol" });
            });
            var repo = new PreferenciaRepositorio(ctx);

            var id = await repo.ObtenerIdPreferenciaPorNombre("Futbol");

            Assert.Equal(1, id);
        }

        [Fact]
        public async Task ObtenerIdPreferenciaPorNombre_NoExiste_RetornaCero()
        {
            using var ctx = CreateInMemoryContext();
            var repo = new PreferenciaRepositorio(ctx);

            var id = await repo.ObtenerIdPreferenciaPorNombre("NoHay");

            Assert.Equal(0, id);
        }
        [Fact]
        public async Task ActualizarPreferenciasAsync_RemueveYReasigna()
        {
            var usuario = new Datos.EF.Usuario
            {
                IdUsuario = 1,
                Usuario_Preferencia = new List<Usuario_Preferencia>
            {
                new Usuario_Preferencia { IdPreferencia = 10 },
                new Usuario_Preferencia { IdPreferencia = 20 }
            }
            };
            var nuevasDto = new List<PreferenciaDTO>
        {
            new PreferenciaDTO { IdPreferencia = 30 },
            new PreferenciaDTO { IdPreferencia = 40 }
        };
            _mockDbSetUsuarioPreferencia.Setup(d => d.RemoveRange(It.IsAny<IEnumerable<Usuario_Preferencia>>()));
            await _repositorio.ActualizarPreferenciasAsync(usuario, nuevasDto);

            _mockDbSetUsuarioPreferencia.Verify(d => d.RemoveRange(
                It.Is<IEnumerable<Usuario_Preferencia>>(col =>
                    col.Count() == 2 &&
                    col.Any(up => up.IdPreferencia == 10) &&
                    col.Any(up => up.IdPreferencia == 20)
                )
            ), Times.Once);

            Assert.Collection(usuario.Usuario_Preferencia,
                up => Assert.Equal(30, up.IdPreferencia),
                up => Assert.Equal(40, up.IdPreferencia)
            );
        }

        [Fact]
        public async Task ActualizarPreferenciasAsync_ConListaVacia_SigueSinErrores()
        {
            var mockCtx = new Mock<HangoContext>();
            var mockDbUsuPref = new Mock<DbSet<Usuario_Preferencia>>();
            mockCtx.Setup(c => c.Usuario_Preferencia).Returns(mockDbUsuPref.Object);
            var repo = new PreferenciaRepositorio(mockCtx.Object);

            var usuario = new Datos.EF.Usuario
            {
                IdUsuario = 2,
                Usuario_Preferencia = new List<Usuario_Preferencia>()
            };
            await repo.ActualizarPreferenciasAsync(usuario, new List<PreferenciaDTO>());

            mockDbUsuPref.Verify(d => d.RemoveRange(It.IsAny<IEnumerable<Usuario_Preferencia>>()), Times.Once);
            Assert.Empty(usuario.Usuario_Preferencia);
        }

        [Fact]
        public async Task GuardarPreferenciaDelUsuario_CaminoFeliz_AgregaYRetorna()
        {
            var mockCtx = new Mock<HangoContext>();
            var mockDbUsuPref = new Mock<DbSet<Usuario_Preferencia>>();
            mockCtx.Setup(c => c.Usuario_Preferencia).Returns(mockDbUsuPref.Object);
            var repo = new PreferenciaRepositorio(mockCtx.Object);

            var up = new Usuario_Preferencia { IdUsuario = 5, IdPreferencia = 8 };

            mockDbUsuPref
                .Setup(d => d.AddAsync(up, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Usuario_Preferencia>)null);

            var result = await repo.GuardarPreferenciaDelUsuario(up);

            Assert.Same(up, result);
            mockDbUsuPref.Verify(d => d.AddAsync(up, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GuardarPreferenciaDelUsuario_Null_ArgumentNullException()
        {
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _repositorio.GuardarPreferenciaDelUsuario(null));

            Assert.IsType<ArgumentNullException>(ex.InnerException);
            Assert.Equal("Las preferencias del usuario no pueden ser nulas. (Parameter 'user')", ex.InnerException.Message);
        }



        private HangoContext CreateInMemoryContext(Action<HangoContext> seed = null)
        {
            var options = new DbContextOptionsBuilder<HangoContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new HangoContext(options);
            seed?.Invoke(ctx);
            ctx.SaveChanges();
            return ctx;
        }
    }
}
