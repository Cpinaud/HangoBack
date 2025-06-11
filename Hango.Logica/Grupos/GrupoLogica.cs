using Hango.Datos.DTO.Preferencia;
using Hango.Datos.DTO.Grupo;
using Hango.Datos.DTO.Integrantes;
using Hango.Datos.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Hango.Repositorios.Grupo;
using Hango.Repositorios.Zona;



namespace Hango.Logica.Grupos { 


    public interface IGrupoLogica
    {
        Task<Grupo> CrearGrupo(GrupoInsertDTO grupo, int idUsuario);
        Task<GrupoDTO?> ObtenerGrupoPorId(int idGrupo,int idUsuario);
        Task<GrupoShortDTO?> ObtenerGrupoPorTokenInvitacion(string token);
        Task<List<GrupoShortDTO>> ObtenerGruposDelUsuario(int idUsuario);
        Task<List<IntegranteDTO>> ObtenerIntegrantesGrupo(int idGrupo, int idUsuario);

        Task<GrupoDTO> ActualizarGrupo(int idGrupo, GrupoUpdateDTO grupo,int idUsuario);
        Task<bool> UnirseAlGrupo(int usuario, int idGrupo);
        Task EliminarIntegranteGrupo(int idGrupo, int idUsuario,int idUserEliminador);
        Task<string> RegenerarTokenInvitacionGrupo(int idGrupo, int idSolicitante);

        Task<List<Hango.Datos.EF.Planes>> ObtenerPlanesConfirmadosDelGrupo(int idGrupo,int idUsuario);
        Task setearPreferenciasGrupoAuto(int idGrupo, int idUsuario);
        Task ActivarGrupo(int idGrupo, int idUsuario);

    }


    public class GrupoLogica : IGrupoLogica
    {
        private readonly IGrupoRepositorio _grupoRepositorio;
        private readonly IZonaRepositorio _zonaRepositorio;
        private readonly HangoContext _context; //necesario para el manejo de transacciones (sólo se tiene que usar para eso en este archivo)
        private readonly int limitePreferencias = 5;
        private readonly int limiteZonas = 3;
        private readonly int valorPrioridadAuto = 0;
        private readonly int valorPrioridadAgregaManual = 1;
        private readonly int valorPrioridadEliminaManual = 2;
        

        public GrupoLogica(IGrupoRepositorio grupoRepositorio, HangoContext context,IZonaRepositorio zonaRepositorio)
        {
            _grupoRepositorio = grupoRepositorio;
            _context = context;
            _zonaRepositorio = zonaRepositorio;
        }

        public async Task<GrupoDTO?> ObtenerGrupoPorId(int idGrupo,int idUsuario)
        {
            var usuarioPertenece = await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
            if (!usuarioPertenece)
                throw new Exception("El usuario no pertenece al grupo");
            try
            {
                var grupo=  await _grupoRepositorio.ObtenerGrupoPorId(idGrupo);
                var grupoDTO = CrearGrupoDTO(grupo);

                return grupoDTO;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public async Task<Grupo> CrearGrupo(GrupoInsertDTO grupo, int idUsuario)
        {

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nuevoGrupo = new Grupo
                {
                    Nombre = grupo.Nombre,
                    TokenInvitacion = Guid.NewGuid().ToString(),
                    CreateAt = DateTime.Now,
                    Grupo_Preferencia = new List<Grupo_Preferencia>()
                };

                if (! await _grupoRepositorio.DatosUsuarioSeteados(idUsuario))
                {
                    throw new Exception("No puede crear un grupo, perfil incompleto.");
                }

                nuevoGrupo.UrlImagen = await GuardarImagenAsync(grupo.Imagen);

                await _grupoRepositorio.GuardarGrupo(nuevoGrupo);

                await setearZonasGrupo(grupo.Zonas,nuevoGrupo);

                //Grabo la relacion usuario-grupo con el usuario como admin del mismo
                var usuarioGrupo = new Usuario_Grupo
                {
                    IdUsuario = idUsuario,
                    IdGrupo = nuevoGrupo.IdGrupo,
                    Administrador = true,
                    Ingreso = DateTime.Now
                };
                await _grupoRepositorio.GuardarUsuarioGrupo(usuarioGrupo);

                await setearPreferenciasGrupoAuto(nuevoGrupo.IdGrupo, idUsuario);
                await transaction.CommitAsync();
                return nuevoGrupo;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

      

        public async Task<List<GrupoShortDTO>> ObtenerGruposDelUsuario(int idUsuario)
        {

            try
            {
                var gruposObtenidos = await _grupoRepositorio.ObtenerGruposDeUnUsuario(idUsuario);
                var grupos = gruposObtenidos.Select(g => new GrupoShortDTO
                 {
                     IdGrupo = g.IdGrupo,
                     Nombre = g.Nombre,
                     UrlImagen = g.UrlImagen,
                     Zonas = g.Zonas_Grupos.Select(zg => new Zonas
                     {
                         Id = zg.IdZona,
                         Nombre = zg.IdZonaNavigation.Nombre
                     }).ToList(),
                     Integrantes = g.Usuario_Grupo.Select(ug2 => new IntegranteDTO
                     {
                         IdUsuario = ug2.IdUsuario,
                         Nombre = ug2.IdUsuarioNavigation.Nombre,
                         Administrador = ug2.Administrador
                     }).ToList()

                 }).ToList();

                return grupos;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public async Task<List<IntegranteDTO>> ObtenerIntegrantesGrupo(int idGrupo, int idUsuario)
        {
            try
            {
                var usuarioPertenece = await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
                if (!usuarioPertenece)
                    throw new Exception("El usuario no pertenece al grupo");
                var Integrantes = await _grupoRepositorio.ObtenerIntegrantesGrupo(idGrupo);
                return Integrantes.Select(ug => new IntegranteDTO
                {
                    IdUsuario = ug.IdUsuario,
                    Nombre = ug.IdUsuarioNavigation.Nombre,
                    Administrador = ug.Administrador
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<GrupoDTO> ActualizarGrupo(int idGrupo, GrupoUpdateDTO grupoDTO,int idUsuario)
        {
            var usuarioPertenece= await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
            if (!usuarioPertenece)
                throw new Exception("El usuario no pertenece al grupo");

            if (!validarActualizacionPreferencias(grupoDTO.Preferencias))
                throw new Exception($"No se pueden agregar más de {limitePreferencias} preferencias al grupo.");
            if (grupoDTO.Nombre != null) { 
                if (grupoDTO.Nombre.Length > 150)
                    throw new Exception("El nombre ingresado no es válido");
                }

            var grupo = await _grupoRepositorio.ObtenerGrupoPorId(idGrupo);
            if (grupo == null)
                throw new Exception("El grupo no existe");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (grupoDTO.Zonas != null && grupoDTO.Zonas.Any())
                    await actualizarZonasGrupo(grupo, grupoDTO);
                if (grupoDTO.Preferencias != null && grupoDTO.Preferencias.Any())
                    await actualizarPreferenciasGrupo(grupo, grupoDTO);

                if (grupoDTO.Nombre != null)
                    grupo.Nombre = grupoDTO.Nombre;
                if (grupoDTO.Imagen != null)
                {
                    var nuevaUrlImagen = await GuardarImagenAsync(grupoDTO.Imagen);
                    grupo.UrlImagen = nuevaUrlImagen;
                }

                await _grupoRepositorio.ActualizarGrupo(grupo);
                await transaction.CommitAsync();

                var grupoActualizado = await _grupoRepositorio.ObtenerGrupoPorId(grupo.IdGrupo);
                var grupoDto = CrearGrupoDTO(grupoActualizado);
                return grupoDto;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        
        public async Task<GrupoShortDTO?> ObtenerGrupoPorTokenInvitacion(string token)
        {

            var grupo = await _grupoRepositorio.ObtenerGrupoPorToken(token);
            var grupoShort = new GrupoShortDTO
             {
              IdGrupo = grupo.IdGrupo,
              Nombre = grupo.Nombre,
              UrlImagen = grupo.UrlImagen
            };
            if (grupoShort == null)
                return null;
            return grupoShort;
        }



        public async Task<bool> UnirseAlGrupo(int idUsuario, int idGrupo)
        {
            var usuarioPertenece = await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
            if (usuarioPertenece)
                throw new Exception("El usuario ya pertenece al grupo");

            if (!await _grupoRepositorio.DatosUsuarioSeteados(idUsuario))
            {
                throw new Exception("No puede unirse al grupo, perfil incompleto.");
            }
            try
            {
                var usuarioGrupo = new Usuario_Grupo
                {
                    IdUsuario = idUsuario,
                    IdGrupo = idGrupo,
                    Administrador = false,
                    Ingreso = DateTime.Now
                };

                await _grupoRepositorio.GuardarUsuarioGrupo(usuarioGrupo);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ocurrió un problema." + ex.Message, ex);
            }


        }


        

        public async Task EliminarIntegranteGrupo(int idGrupo, int idUsuario, int idUserEliminador)
        {

            if (!await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo))
                throw new Exception("El usuario que intenta eliminar no pertenece al grupo");

            //valido si el usuario QUE EJECUTA LA ACCION es Admin
            if (!await _grupoRepositorio.UsuarioEsAdminDeGrupo(idUserEliminador, idGrupo) && idUsuario != idUserEliminador)
                throw new Exception("No tiene permisos para realizar esta acción");

            var integranteAEliminar = await _grupoRepositorio.ObtenerIntegranteDeUnGrupoPorId(idGrupo, idUsuario);
                

            //valido si el usuario QUE SE VA A ELIMINAR es Admin (abandona)
            var eliminadoEsAdmin = await _grupoRepositorio.UsuarioEsAdminDeGrupo(idUsuario, idGrupo);
            var unicoIntegrante = await _grupoRepositorio.GrupoConUnicoIntegrante(idGrupo);

            if (eliminadoEsAdmin && unicoIntegrante)
                throw new Exception("No puede abandonar siendo el único integrante. Se debe eliminar el grupo");
            var grupoConUnSoloAdmin = await _grupoRepositorio.GrupoConUnicoAdmin(idGrupo);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (eliminadoEsAdmin && grupoConUnSoloAdmin)
                    await _grupoRepositorio.ReemplazarAdminGrupo(idGrupo);

                await _grupoRepositorio.EliminarIntegranteDelGrupo(integranteAEliminar);
                

                await transaction.CommitAsync();
            }catch (Exception) {
                await transaction.RollbackAsync();
                throw new Exception("No se pudo eliminar al usuario");
            }

        }

        public async Task<string> RegenerarTokenInvitacionGrupo(int idGrupo, int idSolicitante)
        {
            
            var grupo = await _grupoRepositorio.ObtenerGrupoPorId(idGrupo);
            if (grupo == null)
                throw new Exception("Grupo no encontrado");

            if (!await _grupoRepositorio.UsuarioEsAdminDeGrupo(idSolicitante, idGrupo))
                throw new Exception("No tiene permisos para realizar esta acción");

            try
            {
                var token = Guid.NewGuid().ToString();
                await _grupoRepositorio.ActualizarToken(idGrupo,token);
                
                return token;
            }
            catch (Exception ex)
            {
                throw new Exception("Error inesperado al regenerar y guardar el token", ex);
            }
        }

        public async Task<List<Hango.Datos.EF.Planes>> ObtenerPlanesConfirmadosDelGrupo(int idGrupo,int idUsuario)
        {
            var usuarioPertenece = await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
            if (!usuarioPertenece)
                throw new Exception("El usuario no pertenece al grupo");
            var planes = await _grupoRepositorio.ObtenerPlanesConfirmadosDelGrupo(idGrupo);
            return planes;

        }


        ///MÉTODOS PRIVADOS
        

    private async Task<string> GuardarImagenAsync(IFormFile imagen)
        {
            if (imagen == null)
                return "/avatar-group/default.png";

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(imagen.FileName).ToLower();

            if (!extensionesPermitidas.Contains(extension))
                throw new ArgumentException("Formato de imagen no permitido.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "grupos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            return $"/uploads/grupos/{uniqueFileName}";
        }


        private async Task setearZonasGrupo(List<string> zonas, Grupo grupo)
        {
            if (zonas.Count > limiteZonas)
                throw new Exception($"No se pueden agregar más de {limiteZonas} zonas al grupo.");
            foreach (var Zona in zonas)
            {
                int ZonaInt = int.Parse(Zona);

                var zonaIngresadaExistente = await _zonaRepositorio.ObtenerZonaPorId(ZonaInt);
                if (zonaIngresadaExistente == null)
                    throw new Exception("No se pudo encontrar la zona");
                else
                {
                    //var idZonaCorrecto = zonaIngresadaExistente.Id;
                    var zonaGrupo = new Zonas_Grupos
                    {
                        IdGrupo = grupo.IdGrupo,
                        IdZona = ZonaInt
                    };
                    await _zonaRepositorio.GuardarZonaEnGrupo(zonaGrupo);
                }

            }
        }



        private async Task setearPreferenciasGrupoAuto(int idGrupo, int idUsuario)
        {
            var preferenciasGrupoAgregadas = await _grupoRepositorio.ObtenerIdPreferenciasGrupoPorTipo(idGrupo, valorPrioridadAgregaManual);

            if (preferenciasGrupoAgregadas.Count < limitePreferencias)
            {
                try
                {
                    List<Usuario_Grupo> integrantes;
                    List<int> idIntegrantes;
                    List<int> idPreferenciasComunes;
                    List<Grupo_Preferencia> preferenciasAutoGrupo;

                    integrantes = await _grupoRepositorio.ObtenerIntegrantesGrupo(idGrupo);
                    idIntegrantes = integrantes.Select(i => i.IdUsuario).ToList();
                    await Task.Yield();
                    //Busco las preferencias comunes entre los integrantes (que no hayan sido eliminadas/agregadas manualmente)
                    //ordenadas de la más a menos común
                    idPreferenciasComunes = await _grupoRepositorio.ObtenerIdPreferenciasComunesOrdenadas(idGrupo, idIntegrantes);
                    await Task.Yield();
                    //elimino las preferencias del grupo que tienen prioridad=0 (automáticas)
                    preferenciasAutoGrupo = await _grupoRepositorio.ObtenerPreferenciasGrupoPorTipo(idGrupo, valorPrioridadAuto);

                    var disponibles = limitePreferencias - preferenciasAutoGrupo.Count();

                    await _grupoRepositorio.EliminarPreferenciasGrupoAuto(idGrupo);

                    //agrego las preferencias auto al grupo
                    foreach (var preferenciaId in idPreferenciasComunes)
                    {
                        if (disponibles == 0) break;

                        Grupo_Preferencia grupoPreferencia = new Grupo_Preferencia
                        {
                            IdGrupo = idGrupo,
                            IdPreferencia = preferenciaId,
                            Prioridad = 0
                        };
                        await _grupoRepositorio.GuardarPreferenciaGrupo(grupoPreferencia);
                        disponibles--;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        private async Task actualizarZonasGrupo(Grupo grupo, GrupoUpdateDTO grupoDto)
        {
            await _grupoRepositorio.VaciarZonasDelGrupo(grupo);

            foreach (var zonaDto in grupoDto.Zonas)
            {
                var zona = await _zonaRepositorio.ObtenerZonaPorId(zonaDto.Id);

                if (zona != null)
                {
                    Zonas_Grupos zonaGrupo = new Zonas_Grupos
                    {
                        IdGrupo = grupo.IdGrupo,
                        IdZona = zona.Id
                    };
                    await _zonaRepositorio.GuardarZonaEnGrupo(zonaGrupo);
                }
            }
        }

        private bool validarActualizacionPreferencias(List<PreferenciaDTO> preferencias)
        {
            var preferenciasAInsertarOActualizar = preferencias
            .Count(p => p.Prioridad == 0 || p.Prioridad == 1);

            if (preferenciasAInsertarOActualizar > limitePreferencias)
            {
                return false;
            }
            return true;
        }

        private GrupoDTO CrearGrupoDTO(Grupo grupo)
        {
            var grupoDTO = new GrupoDTO
            {
                IdGrupo = grupo.IdGrupo,
                Nombre = grupo.Nombre,
                UrlImagen = grupo.UrlImagen,
                TokenInvitacion = $"http://localhost:5222/Grupos/invitacion/{grupo.TokenInvitacion}",
                Activo = grupo.Activo,

                Zonas = grupo.Zonas_Grupos?
                    .Where(zg => zg?.IdZonaNavigation != null)
                    .Select(zg => new Zonas
                    {
                        Id = zg.IdZona,
                        Nombre = zg.IdZonaNavigation.Nombre
                    }).ToList() ?? new List<Zonas>(),

                Integrantes = grupo.Usuario_Grupo?
                    .Where(ug => ug?.IdUsuarioNavigation != null)
                    .Select(ug => new IntegranteDTO
                    {
                        IdUsuario = ug.IdUsuario,
                        Nombre = ug.IdUsuarioNavigation.Nombre,
                        Administrador = ug.Administrador
                    }).ToList() ?? new List<IntegranteDTO>(),

                Preferencias = grupo.Grupo_Preferencia?
                    .Where(gp => gp?.Prioridad != valorPrioridadEliminaManual && gp?.IdPreferenciaNavigation != null)
                    .Select(gp => new PreferenciaDTO
                    {
                        IdPreferencia = gp.IdPreferencia,
                        Nombre = gp.IdPreferenciaNavigation.Nombre,
                        Prioridad = (int)gp.Prioridad
                    }).ToList() ?? new List<PreferenciaDTO>()
            };

            return grupoDTO;
        }

        private async Task actualizarPreferenciasGrupo(Grupo grupo, GrupoUpdateDTO grupoDto)
        {

            var preferenciasActuales = await _grupoRepositorio.ObtenerPreferenciasGrupo(grupo.IdGrupo);

            foreach (var prefDto in grupoDto.Preferencias)
            {
                var existente = preferenciasActuales
                    .FirstOrDefault(p => p.IdPreferencia == prefDto.IdPreferencia);

                if (existente != null)
                {
                    // Ya existe la preferencia para el grupo, actualizo.
                    if (existente.Prioridad != prefDto.Prioridad)
                    {

                        await _grupoRepositorio.ActualizarPreferenciaGrupo(existente,prefDto.Prioridad);
                    }
                }
                else
                {
                    //la preferencia no existe para el grupo, la agrego.
                    var nuevaPreferencia = new Grupo_Preferencia
                    {
                        IdGrupo = grupo.IdGrupo,
                        IdPreferencia = prefDto.IdPreferencia,
                        Prioridad = prefDto.Prioridad
                    };
                   await _grupoRepositorio.GuardarPreferenciaGrupo(nuevaPreferencia);
                }
            }
        }

        public async Task ActivarGrupo(int idGrupo,int idUsuario)
        {
            var grupo = await _grupoRepositorio.ObtenerGrupoPorId(idGrupo);
            if (grupo == null)
                throw new Exception("Grupo no encontrado");
            var usuarioPertenece = await _grupoRepositorio.UsuarioPerteneceAlGrupo(idUsuario, idGrupo);
            if (!usuarioPertenece)
                throw new Exception("El usuario no pertenece al grupo");
            var integrantes = await _grupoRepositorio.ObtenerIntegrantesGrupo(idGrupo);
            var cantidadDeIntegrantes = integrantes.Count;
            if (cantidadDeIntegrantes < 2)
            {
                throw new Exception("No se puede activar un grupo con menos de 2 integrantes");
            }
            else
            {
                await _grupoRepositorio.ActivarGrupo(idGrupo);
            }

        }

        Task IGrupoLogica.setearPreferenciasGrupoAuto(int idGrupo, int idUsuario)
        {
            return setearPreferenciasGrupoAuto(idGrupo, idUsuario);
        }
    }
}





