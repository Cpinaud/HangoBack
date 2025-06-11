IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Hango')
BEGIN
    CREATE DATABASE Hango
    COLLATE Latin1_General_CI_AS;  
END
GO

USE Hango;
GO


CREATE TABLE Usuario (
    IdUsuario INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
	Password_Hash Nvarchar(150) NOT NULL,
    IdAvatar INT NOT NULL DEFAULT 1,
    HorariosPrivados BIT DEFAULT 0,
	EstaVerificado BIT DEFAULT 0,
	CodigoVerificacion NVARCHAR(250) null,
	FechaVerificacion DATETIME null,
    AccessTokenMercadoPago VARCHAR(255) NULL,
    CreateAt DATETIME DEFAULT GETDATE(),
    UpdateAt DATETIME DEFAULT GETDATE()
);

	CREATE TABLE Grupo(
	IdGrupo int primary key identity(1,1),
	Nombre NVARCHAR(150) NOT NULL,
	UrlImagen NVARCHAR(200) not NULL,
    Activo BIT not null DEFAULT 0,
	TokenInvitacion nvarchar(200) NOT NULL,
	CreateAt DATETIME DEFAULT GETDATE(),
    UpdateAt DATETIME DEFAULT GETDATE()
	);

	CREATE TABLE Zonas (
		Id INT PRIMARY KEY IDENTITY(1,1),
		Nombre NVARCHAR(100) NOT NULL
	);

CREATE TABLE Zonas_Grupos (
	Id INT PRIMARY KEY IDENTITY(1,1),
	IdZona INT NOT NULL,
	IdGrupo INT NOT NULL,
	CONSTRAINT FK_Zonas_Grupos_Zonas FOREIGN KEY (IdZona) REFERENCES Zonas(Id),
	CONSTRAINT FK_Zonas_Grupos_Grupo FOREIGN KEY (IdGrupo) REFERENCES Grupo(IdGrupo)
);

CREATE TABLE RefreshToken (
    Id INT PRIMARY KEY IDENTITY(1,1),
    IdUsuario INT NOT NULL,
    Token NVARCHAR(500) NOT NULL,
    HechoToken DATETIME NOT NULL,
    ExpiroToken DATETIME NOT NULL,
	RevocarToken DATETIME  NULL,
    CONSTRAINT FK_RefreshToken_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario)
);

CREATE TABLE ExcepcionLibre (
	IdExcepcionLibre int primary key identity(1,1),
	IdUsuario int not null,
	Fecha Datetime not null,
	HorarioInicio Datetime not null,
	HorarioFin Datetime not null,
	CONSTRAINT Fk_ExcepcionLibre_Usuario FOREIGN KEY(IdUsuario) REFERENCES Usuario(IdUsuario)
	);

	CREATE TABLE ExcepcionOcupado (
	IdExcepcionOcupado int primary key identity(1,1),
	IdUsuario int not null,
	Fecha Datetime not null,
	HorarioInicio Datetime not null,
	HorarioFin Datetime not null,
	CONSTRAINT Fk_ExcepcionOcupado_Usuario FOREIGN KEY(IdUsuario) REFERENCES Usuario(IdUsuario)
	);

	CREATE TABLE Preferencia (
	IdPreferencia int primary key identity(1, 1),
	Nombre nvarchar(100) not null
	)

	CREATE TABLE Usuario_Preferencia (
	Id int primary key identity(1,1),
	IdUsuario int not null,
	IdPreferencia int not null,
	CONSTRAINT FK_Usuario_Preferencia_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
    CONSTRAINT FK_Usuario_Preferencia_Preferencia FOREIGN KEY (IdPreferencia) REFERENCES Preferencia(IdPreferencia),
    CONSTRAINT UQ_Usuario_Preferencia UNIQUE (IdUsuario, IdPreferencia)
	)

	CREATE TABLE Usuario_Grupo (
	Id int primary key identity(1,1),
	IdUsuario int not null,
	IdGrupo int not null,
	Administrador bit not null,
	Favorito bit null,
	Ingreso datetime default getdate(),
	CONSTRAINT FK_Usuario_Grupo_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
    CONSTRAINT FK_Usuario_Grupo_Grupo FOREIGN KEY (IdGrupo) REFERENCES Grupo(IdGrupo),
    CONSTRAINT UQ_Usuario_Grupo UNIQUE (IdUsuario, IdGrupo)
	)

    CREATE TABLE Evento (
    IdEvento INT IDENTITY(1,1) PRIMARY KEY,
    GrupoId INT NOT NULL,
    UsuarioId INT NOT NULL, -- Accionador del evento
    Fecha DATETIME NOT NULL,
    TipoEvento VARCHAR(50) NOT NULL, --'SUGERENCIA', 'DIVISION', 'MENSAJE'.
    IdEventoRelacionado INT NULL, -- Para eventos relacionados como sugerencias o divisiones //FALTA IMPLEMENTAR
    Contenido NVARCHAR(MAX) NOT NULL
);
	
	CREATE TABLE Grupo_Preferencia (
	Id int primary key identity(1,1),
	IdGrupo int not null,
	IdPreferencia int not null,
	Prioridad int,
	CONSTRAINT FK_Grupo_Preferencia_Grupo FOREIGN KEY (IdGrupo) REFERENCES Grupo(IdGrupo),
    CONSTRAINT FK_Grupo_Preferencia_Preferencia FOREIGN KEY (IdPreferencia) REFERENCES Preferencia(IdPreferencia),
    CONSTRAINT UQ_Grupo_Preferencia UNIQUE (IdGrupo, IdPreferencia)
	)


	CREATE TABLE Presupuesto (
    IdPresupuesto INT IDENTITY PRIMARY KEY,
    Estimado      DECIMAL(12,2) NOT NULL,
    Rango         TINYINT       NOT NULL,    
    FechaCreacion DATETIME DEFAULT GETDATE(),
    IdUsuario     INT NOT NULL,
    CONSTRAINT FK_Presupuesto_Usuario
        FOREIGN KEY (IdUsuario) REFERENCES Usuario (IdUsuario)
		);



CREATE TABLE HorarioDisponible (
    [IdHorarioDisponible] INT NOT NULL PRIMARY KEY identity(1,1), 
    [IdUsuario] INT NOT NULL,
    [Fecha] DATETIME NOT NULL,
    [DiaSemana] TINYINT NOT NULL,
    [HorarioInicio] TIME NOT NULL,
    [HorarioFin] TIME NOT NULL,
    CONSTRAINT [Fk_HorarioDisponible_Usuario] FOREIGN KEY ([IdUsuario])
        REFERENCES [dbo].[Usuario] ([IdUsuario])    
);

CREATE TABLE Avatar (
	IdAvatar INT PRIMARY KEY IDENTITY(1,1),
	Nombre NVARCHAR(255) NOT NULL,
	RutaImagen NVARCHAR(255) NOT NULL
);

CREATE TABLE Propuesta (
    IdPropuesta INT PRIMARY KEY IDENTITY(1,1),
    GrupoId INT NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    FechaVencimiento DATETIME NOT NULL,
    Origen VARCHAR(50) NOT NULL, -- 'IA' o 'Usuario'
    IdEvento INT NULL, -- Para relacionar con eventos si es necesario
    FOREIGN KEY (GrupoId) REFERENCES Grupo(IdGrupo),
    FOREIGN KEY (IdEvento) REFERENCES Evento(IdEvento)
);

CREATE TABLE Planes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PropuestaId INT NOT NULL,
    DiaSemana INT NOT NULL,
    Fecha DATE NOT NULL,
    PreferenciaId INT NOT NULL,
    Hora TIME NOT NULL,
    Lugar VARCHAR(100),
    Descripcion TEXT,
    Direccion VARCHAR(255),
    Estado VARCHAR(20) ,
    Presupuesto VARCHAR(10)

    FOREIGN KEY (PropuestaId) REFERENCES Propuesta(IdPropuesta)
);

CREATE TABLE Usuario_Plan (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UsuarioId INT NOT NULL,
    PlanId INT NOT NULL,
    Estado VARCHAR(20), -- INCLUIDO O NO EN EL PLAN	
    FOREIGN KEY (UsuarioId) REFERENCES Usuario(IdUsuario),
    FOREIGN KEY (PlanId) REFERENCES Planes(Id)
);


CREATE TABLE Usuario_Propuesta (
    IdUsuario INT NOT NULL,
    IdPropuesta INT NOT NULL,
    PRIMARY KEY (IdUsuario, IdPropuesta),
    FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
    FOREIGN KEY (IdPropuesta) REFERENCES Propuesta(IdPropuesta)
);

CREATE TABLE Voto_Plan (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UsuarioId INT NOT NULL,
    PlanId INT NOT NULL,
    Voto INT,
    FechaVoto DATETIME NOT NULL DEFAULT GETDATE(),

    FOREIGN KEY (UsuarioId) REFERENCES Usuario(IdUsuario),
    FOREIGN KEY (PlanId) REFERENCES Planes(Id)
);

CREATE TABLE Cuenta (
    IdCuenta INT PRIMARY KEY IDENTITY(1,1),
    IdPlan INT NOT NULL,
    UsuarioCreadorId INT NOT NULL,
    Descripcion NVARCHAR(255),
    MontoInicial DECIMAL(10, 2),
    FechaCreacion DATETIME DEFAULT GETDATE(),
    FechaCierre DATETIME NULL,
    MontoTotal DECIMAL(18, 2) NULL,
    UsuarioReceptorId INT NULL,
    Estado NVARCHAR(20) DEFAULT 'Abierta', -- Abierta | Cerrada
     ReceptorAceptaMercadoPago BIT DEFAULT 0,
    FOREIGN KEY (IdPlan) REFERENCES Planes(Id),
    FOREIGN KEY (UsuarioCreadorId) REFERENCES Usuario(IdUsuario),
    FOREIGN KEY (UsuarioReceptorId) REFERENCES Usuario(IdUsuario)
);

CREATE TABLE TipoParticipacion (
    IdTipoParticipacion INT PRIMARY KEY IDENTITY(1,1),
    Nombre VARCHAR(50) NOT NULL -- Ejemplo: 'Dividido', 'Individual'
);
CREATE TABLE ParticipacionCuenta (
    IdParticipacion INT PRIMARY KEY IDENTITY(1,1),
    CuentaId INT NOT NULL,
    UsuarioId INT NOT NULL,
    Estado NVARCHAR(20) NOT NULL, -- Participó | NoGastó | NoParticipó
    MontoGastado DECIMAL(10, 2) DEFAULT 0,
    MontoDebe DECIMAL(18,2) NOT NULL DEFAULT 0,
    IdTipoParticipacion INT NOT NULL
    FOREIGN KEY (CuentaId) REFERENCES Cuenta(IdCuenta),
    FOREIGN KEY (UsuarioId) REFERENCES Usuario(IdUsuario),
    FOREIGN KEY (IdTipoParticipacion) REFERENCES TipoParticipacion(IdTipoParticipacion)
);
CREATE TABLE TransferenciaSugerida (
    IdTransferencia INT PRIMARY KEY IDENTITY(1,1),
    CuentaId INT NOT NULL,
    DeUsuarioId INT NOT NULL,
    AUsuarioId INT NOT NULL,
    Monto DECIMAL(10, 2) NOT NULL,
    FOREIGN KEY (CuentaId) REFERENCES Cuenta(IdCuenta),
    FOREIGN KEY (DeUsuarioId) REFERENCES Usuario(IdUsuario),
    FOREIGN KEY (AUsuarioId) REFERENCES Usuario(IdUsuario)
);

CREATE TABLE EventoUsuario (
    Id INT IDENTITY(1,1),
    EventoId INT NOT NULL,
    UsuarioId INT NOT NULL,
    Leido BIT NOT NULL DEFAULT 0,
    PRIMARY KEY (EventoId, UsuarioId),
    FOREIGN KEY (EventoId) REFERENCES Evento(IdEvento),
    FOREIGN KEY (UsuarioId) REFERENCES Usuario(IdUsuario)
);

CREATE TABLE Notificacion (
    IdNotificacion INT PRIMARY KEY IDENTITY(1,1),
    UsuarioId INT NOT NULL,
    EventoId INT NOT NULL,
    FechaEnvio DATETIME NOT NULL DEFAULT GETDATE(),
    Leida BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (UsuarioId) REFERENCES Usuario(IdUsuario),
    FOREIGN KEY (EventoId) REFERENCES Evento(IdEvento)
);


--Insert avatares grupo
SET IDENTITY_INSERT Avatar ON;

INSERT INTO Avatar (IdAvatar, Nombre, RutaImagen) VALUES (1, 'hango', 'avatar-group/hango.png');
SET IDENTITY_INSERT Avatar OFF;

INSERT INTO Avatar (Nombre, RutaImagen) VALUES 
('argentina', 'avatar-group/Argentina.png'),
('art', 'avatar-group/art.png'),
('basket', 'avatar-group/basket.png'),
('beer', 'avatar-group/beer.png'),
('bike', 'avatar-group/bike.png'),
('book', 'avatar-group/book.png'),
('camera', 'avatar-group/camera.png'),
('cart', 'avatar-group/cart.png'),
('coffee', 'avatar-group/coffee.png'),
('cook', 'avatar-group/cook.png'),
('fish', 'avatar-group/fish.png'),
('fishing', 'avatar-group/fishing.png'),
('flower', 'avatar-group/flower.png'),
('football', 'avatar-group/football.png'),
('game', 'avatar-group/game.png'),
('guitar', 'avatar-group/guitar.png'),
('microphone', 'avatar-group/microphone.png'),
('music', 'avatar-group/music.png'),
('plant', 'avatar-group/plant.png'),
('pride', 'avatar-group/pride.png'),
('screen', 'avatar-group/screen.png'),
('snorkel', 'avatar-group/snorkel.png'),
('speakers', 'avatar-group/speakers.png'),
('tent', 'avatar-group/tent.png'),
('tree', 'avatar-group/tree.png');


--INSERT ZONAS
INSERT INTO Zonas (Nombre) VALUES
('Zeballos'),
('Florencio Varela'),
('William C. Morris'),
('Wilde'),
('Virreyes'),
('Virrey del Pino'),
('Villa Vatteone'),
('Villa Urquiza'),
('Villa Tesei'),
('Villa Soldati'),
('Villa Sarmiento'),
('Villa Santa Rita'),
('Villa San Luis'),
('Villa Riachuelo'),
('Villa Real'),
('Villa Raffo'),
('Villa Pueyrredón'),
('Villa Ortúzar'),
('Villa Martelli'),
('Villa Maipú'),
('Villa Madero'),
('Villa Lynch'),
('Villa Luzuriaga'),
('Villa Luro'),
('Villa Lugano'),
('Villa La Florida'),
('Villa General Mitre'),
('Villa Fiorito'),
('Villa España'),
('Villa Domínico'),
('Villa Devoto'),
('Villa del Parque'),
('Villa de Mayo'),
('Villa Crespo'),
('Villa Centenario'),
('Villa Brown'),
('Villa Bosch'),
('Villa Ballester'),
('Villa Adelina'),
('Victoria'),
('Vicente López'),
('Versalles'),
('Vélez Sársfield'),
('Valentín Alsina'),
('Udaondo'),
('Turdera'),
('Trujui'),
('Troncos del Talar'),
('Tristán Suárez'),
('Tortuguitas'),
('Tigre'),
('Temperley'),
('Tapiales'),
('Sourigues'),
('Sarandí'),
('Santos Lugares'),
('Santa Rosa'),
('San Telmo'),
('San Nicolás'),
('San Miguel'),
('San Martín'),
('San Justo'),
('San José'),
('San Isidro'),
('San Francisco Solano'),
('San Fernando'),
('San Cristóbal'),
('San Antonio de Padua'),
('San Andrés'),
('Sáenz Peña'),
('Saavedra'),
('Rincón de Milberg'),
('Ricardo Rojas'),
('Retiro'),
('Remedios de Escalada'),
('Recoleta'),
('Ranelagh'),
('Ramos Mejía'),
('Rafael Castillo'),
('Rafael Calzada'),
('Quilmes Oeste'),
('Quilmes'),
('Puerto Madero'),
('Pontevedra'),
('Plátanos'),
('Piñeyro'),
('Pereyra'),
('Paso del Rey'),
('Parque San Martín'),
('Parque Patricios'),
('Parque Chas'),
('Parque Chacabuco'),
('Parque Avellaneda'),
('Palermo'),
('Pablo Podestá'),
('Pablo Nogués'),
('Once de Septiembre'),
('Olivos'),
('Núñez'),
('Nueva Pompeya'),
('Munro'),
('Muñiz'),
('Morón'),
('Moreno'),
('Monte Grande'),
('Monte Chingolo'),
('Monte Castro'),
('Monserrat'),
('Ministro Rivadavia'),
('Merlo'),
('Mataderos'),
('Martínez'),
('Martín Coronado'),
('Mariano Acosta'),
('Malvinas Argentinas'),
('Luis Guillón'),
('Los Polvorines'),
('Longchamps'),
('Lomas del Mirador'),
('Lomas de Zamora'),
('Loma Hermosa'),
('Llavallol'),
('Liniers'),
('Libertad'),
('Lanús Oeste'),
('Lanús'),
('La Unión'),
('La Tablada'),
('La Reja'),
('La Paternal'),
('La Lucila'),
('La Capilla'),
('La Boca'),
('Juan María Gutiérrez'),
('José Mármol'),
('José María Ezeiza'),
('José León Suárez'),
('José Ingenieros'),
('José C. Paz'),
('Ituzaingó'),
('Isidro Casanova'),
('Ingeniero Budge'),
('Ingeniero Allan'),
('Ingeniero Adolfo Sourdeaux'),
('Hurlingham'),
('Haedo'),
('Guillermo Hudson'),
('Gregorio de Laferrere'),
('Grand Bourg'),
('González Catán'),
('Gobernador Costa'),
('Glew'),
('Gerli'),
('General Pacheco'),
('Francisco Álvarez'),
('Florida Oeste'),
('Florida'),
('Floresta'),
('Flores'),
('Ezpeleta Oeste'),
('Ezpeleta'),
('Esteban Echeverría'),
('El Talar'),
('El Pato'),
('El Palomar'),
('El Libertador'),
('El Jagüel'),
('Don Torcuato'),
('Don Orione'),
('Don Bosco');



-- INSERT PREFERENCIAS
INSERT INTO Preferencia (Nombre) VALUES ('Tenis');
INSERT INTO Preferencia (Nombre) VALUES ('Paddle');
INSERT INTO Preferencia (Nombre) VALUES ('Running');
INSERT INTO Preferencia (Nombre) VALUES ('Bicicleta');
INSERT INTO Preferencia (Nombre) VALUES ('Caminata');
INSERT INTO Preferencia (Nombre) VALUES ('Trekking');
INSERT INTO Preferencia (Nombre) VALUES ('Yoga');
INSERT INTO Preferencia (Nombre) VALUES ('Pilates');
INSERT INTO Preferencia (Nombre) VALUES ('Crossfit');
INSERT INTO Preferencia (Nombre) VALUES ('Entrenamiento');
INSERT INTO Preferencia (Nombre) VALUES ('Patinaje');
INSERT INTO Preferencia (Nombre) VALUES ('Skate');
INSERT INTO Preferencia (Nombre) VALUES ('Natación');
INSERT INTO Preferencia (Nombre) VALUES ('Paintball');
INSERT INTO Preferencia (Nombre) VALUES ('Bowling');
INSERT INTO Preferencia (Nombre) VALUES ('Pool');
INSERT INTO Preferencia (Nombre) VALUES ('Escape room');
INSERT INTO Preferencia (Nombre) VALUES ('Gaming night');
INSERT INTO Preferencia (Nombre) VALUES ('Películas');
INSERT INTO Preferencia (Nombre) VALUES ('Series');
INSERT INTO Preferencia (Nombre) VALUES ('Karaoke');
INSERT INTO Preferencia (Nombre) VALUES ('Fiesta en casa');
INSERT INTO Preferencia (Nombre) VALUES ('Boliche');
INSERT INTO Preferencia (Nombre) VALUES ('Bar tranqui');
INSERT INTO Preferencia (Nombre) VALUES ('Cena');
INSERT INTO Preferencia (Nombre) VALUES ('Merienda');
INSERT INTO Preferencia (Nombre) VALUES ('Brunch');
INSERT INTO Preferencia (Nombre) VALUES ('Desayuno');
INSERT INTO Preferencia (Nombre) VALUES ('Parrillada');
INSERT INTO Preferencia (Nombre) VALUES ('Picada');
INSERT INTO Preferencia (Nombre) VALUES ('Comidas típicas');
INSERT INTO Preferencia (Nombre) VALUES ('Cata de vinos');
INSERT INTO Preferencia (Nombre) VALUES ('Cerveza');
INSERT INTO Preferencia (Nombre) VALUES ('Café');
INSERT INTO Preferencia (Nombre) VALUES ('Cine');
INSERT INTO Preferencia (Nombre) VALUES ('Teatro');
INSERT INTO Preferencia (Nombre) VALUES ('Recitales');
INSERT INTO Preferencia (Nombre) VALUES ('Festival');
INSERT INTO Preferencia (Nombre) VALUES ('Museo');
INSERT INTO Preferencia (Nombre) VALUES ('Ferias');
INSERT INTO Preferencia (Nombre) VALUES ('Parque');
INSERT INTO Preferencia (Nombre) VALUES ('Día de campo');
INSERT INTO Preferencia (Nombre) VALUES ('Visitar pueblos');
INSERT INTO Preferencia (Nombre) VALUES ('Spa');

INSERT INTO TipoParticipacion (Nombre) VALUES ('Dividido');
INSERT INTO TipoParticipacion (Nombre) VALUES ('Individual');

