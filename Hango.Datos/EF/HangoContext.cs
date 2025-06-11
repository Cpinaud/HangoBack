using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Hango.Datos.EF;

public partial class HangoContext : DbContext
{
    public HangoContext()
    {
    }

    public HangoContext(DbContextOptions<HangoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Avatar> Avatar { get; set; }

    public virtual DbSet<Cuenta> Cuenta { get; set; }

    public virtual DbSet<Evento> Evento { get; set; }

    public virtual DbSet<EventoUsuario> EventoUsuario { get; set; }

    public virtual DbSet<ExcepcionLibre> ExcepcionLibre { get; set; }

    public virtual DbSet<ExcepcionOcupado> ExcepcionOcupado { get; set; }

    public virtual DbSet<Grupo> Grupo { get; set; }

    public virtual DbSet<Grupo_Preferencia> Grupo_Preferencia { get; set; }

    public virtual DbSet<HorarioDisponible> HorarioDisponible { get; set; }

    public virtual DbSet<Notificacion> Notificacion { get; set; }

    public virtual DbSet<ParticipacionCuenta> ParticipacionCuenta { get; set; }

    public virtual DbSet<Planes> Planes { get; set; }

    public virtual DbSet<Preferencia> Preferencia { get; set; }

    public virtual DbSet<Presupuesto> Presupuesto { get; set; }

    public virtual DbSet<Propuesta> Propuesta { get; set; }

    public virtual DbSet<RefreshToken> RefreshToken { get; set; }

    public virtual DbSet<TipoParticipacion> TipoParticipacion { get; set; }

    public virtual DbSet<TransferenciaSugerida> TransferenciaSugerida { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    public virtual DbSet<Usuario_Grupo> Usuario_Grupo { get; set; }

    public virtual DbSet<Usuario_Plan> Usuario_Plan { get; set; }

    public virtual DbSet<Usuario_Preferencia> Usuario_Preferencia { get; set; }

    public virtual DbSet<Voto_Plan> Voto_Plan { get; set; }

    public virtual DbSet<Zonas> Zonas { get; set; }

    public virtual DbSet<Zonas_Grupos> Zonas_Grupos { get; set; }

    public virtual DbSet<Usuario_Propuesta> Usuario_Propuesta { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_CI_AS");

        modelBuilder.Entity<Avatar>(entity =>
        {
            entity.HasKey(e => e.IdAvatar).HasName("PK__Avatar__58BB37B0F73C563D");
            entity.Property(e => e.Nombre).HasMaxLength(255);
            entity.Property(e => e.RutaImagen).HasMaxLength(255);
        });

        modelBuilder.Entity<Cuenta>(entity =>
        {
            entity.HasKey(e => e.IdCuenta).HasName("PK__Cuenta__D41FD706227E9680");

            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Abierta");
            entity.Property(e => e.FechaCierre).HasColumnType("datetime");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MontoInicial).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MontoTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReceptorAceptaMercadoPago)
                 .HasColumnType("bit")
                 .HasDefaultValue(false);

            entity.HasOne(d => d.IdPlanNavigation).WithMany(p => p.Cuenta)
                .HasForeignKey(d => d.IdPlan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cuenta__IdPlan__6E01572D");

            entity.HasOne(d => d.UsuarioCreador).WithMany(p => p.CuentaUsuarioCreador)
                .HasForeignKey(d => d.UsuarioCreadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cuenta__UsuarioC__6EF57B66");

            entity.HasOne(d => d.UsuarioReceptor).WithMany(p => p.CuentaUsuarioReceptor)
                .HasForeignKey(d => d.UsuarioReceptorId)
                .HasConstraintName("FK__Cuenta__UsuarioR__6FE99F9F");
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.IdEvento).HasName("PK__Evento__034EFC043AED0098");

            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.TipoEvento)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EventoUsuario>(entity =>
        {
            entity.HasKey(e => new { e.EventoId, e.UsuarioId }).HasName("PK__EventoUs__8C58875A63189D18");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(d => d.Evento).WithMany(p => p.EventoUsuario)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventoUsu__Event__02FC7413");

            entity.HasOne(d => d.Usuario).WithMany(p => p.EventoUsuario)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventoUsu__Usuar__03F0984C");
        });

        modelBuilder.Entity<ExcepcionLibre>(entity =>
        {
            entity.HasKey(e => e.IdExcepcionLibre).HasName("PK__Excepcio__0C5F73E5DA0FF30C");

            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.HorarioFin).HasColumnType("datetime");
            entity.Property(e => e.HorarioInicio).HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ExcepcionLibre)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_ExcepcionLibre_Usuario");
        });

        modelBuilder.Entity<ExcepcionOcupado>(entity =>
        {
            entity.HasKey(e => e.IdExcepcionOcupado).HasName("PK__Excepcio__508A8EB20D5C2520");

            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.HorarioFin).HasColumnType("datetime");
            entity.Property(e => e.HorarioInicio).HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ExcepcionOcupado)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_ExcepcionOcupado_Usuario");
        });

        modelBuilder.Entity<Grupo>(entity =>
        {
            entity.HasKey(e => e.IdGrupo).HasName("PK__Grupo__303F6FD9E8739481");

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.TokenInvitacion).HasMaxLength(200);
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UrlImagen).HasMaxLength(200);
        });

        modelBuilder.Entity<Grupo_Preferencia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Grupo_Pr__3214EC078D894C1C");

            entity.HasIndex(e => new { e.IdGrupo, e.IdPreferencia }, "UQ_Grupo_Preferencia").IsUnique();

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.Grupo_Preferencia)
                .HasForeignKey(d => d.IdGrupo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grupo_Preferencia_Grupo");

            entity.HasOne(d => d.IdPreferenciaNavigation).WithMany(p => p.Grupo_Preferencia)
                .HasForeignKey(d => d.IdPreferencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grupo_Preferencia_Preferencia");
        });

        modelBuilder.Entity<HorarioDisponible>(entity =>
        {
            entity.HasKey(e => e.IdHorarioDisponible).HasName("PK__HorarioD__51D56E89FA193040");

            entity.Property(e => e.Fecha).HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HorarioDisponible)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_HorarioDisponible_Usuario");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion).HasName("PK__Notifica__F6CA0A852A3C9763");

            entity.Property(e => e.FechaEnvio)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Evento).WithMany(p => p.Notificacion)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificac__Event__09A971A2");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificacion)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificac__Usuar__08B54D69");
        });

        modelBuilder.Entity<ParticipacionCuenta>(entity =>
        {
            entity.HasKey(e => e.IdParticipacion).HasName("PK__Particip__27C41D2454354B01");

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.MontoDebe).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MontoGastado)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Cuenta).WithMany(p => p.ParticipacionCuenta)
                .HasForeignKey(d => d.CuentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Participa__IdTip__76969D2E");

            entity.HasOne(d => d.IdTipoParticipacionNavigation).WithMany(p => p.ParticipacionCuenta)
                .HasForeignKey(d => d.IdTipoParticipacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Participa__IdTip__787EE5A0");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ParticipacionCuenta)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Participa__Usuar__778AC167");
        });

        modelBuilder.Entity<Planes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Planes__3214EC071CE52039");

            entity.Property(e => e.Descripcion).HasColumnType("text");
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Lugar)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Presupuesto)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Propuesta).WithMany(p => p.Planes)
                .HasForeignKey(d => d.PropuestaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Planes__Presupue__5CD6CB2B");
        });

        modelBuilder.Entity<Preferencia>(entity =>
        {
            entity.HasKey(e => e.IdPreferencia).HasName("PK__Preferen__E0146580E2B8A172");

            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Presupuesto>(entity =>
        {
            entity.HasKey(e => e.IdPresupuesto).HasName("PK__Presupue__D70FD1904E74A9A1");

            entity.Property(e => e.Estimado).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Presupuesto)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Presupuesto_Usuario");
        });
        modelBuilder.Entity<Propuesta>(entity =>
        {
            entity.HasKey(e => e.IdPropuesta).HasName("PK__Propuest__1923FEFBA2C63B5B");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaVencimiento).HasColumnType("datetime");
            entity.Property(e => e.Origen)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Grupo).WithMany(p => p.Propuesta)
                .HasForeignKey(d => d.GrupoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propuesta__Grupo__6EF57B66");
            entity.HasOne(p => p.Evento).WithOne() 
                .HasForeignKey<Propuesta>(p => p.IdEvento)
                .HasConstraintName("FK__Propuesta__IdEve__14270015");
        });


        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC079783D60B");

            entity.Property(e => e.ExpiroToken).HasColumnType("datetime");
            entity.Property(e => e.HechoToken).HasColumnType("datetime");
            entity.Property(e => e.RevocarToken).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(500);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.RefreshToken)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshToken_Usuario");
        });

        modelBuilder.Entity<TipoParticipacion>(entity =>
        {
            entity.HasKey(e => e.IdTipoParticipacion).HasName("PK__TipoPart__AB6CDC2C895337FF");

            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TransferenciaSugerida>(entity =>
        {
            entity.HasKey(e => e.IdTransferencia).HasName("PK__Transfer__6E5969EC1CAEE9DF");

            entity.Property(e => e.Monto).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.AUsuario).WithMany(p => p.TransferenciaSugeridaAUsuario)
                .HasForeignKey(d => d.AUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transfere__AUsua__7D439ABD");

            entity.HasOne(d => d.Cuenta).WithMany(p => p.TransferenciaSugerida)
                .HasForeignKey(d => d.CuentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transfere__Cuent__7B5B524B");

            entity.HasOne(d => d.DeUsuario).WithMany(p => p.TransferenciaSugeridaDeUsuario)
                .HasForeignKey(d => d.DeUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transfere__DeUsu__7C4F7684");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__Usuario__5B65BF970CF54757");

            entity.HasIndex(e => e.Email, "UQ__Usuario__A9D105342DB4A8D2").IsUnique();

            entity.Property(e => e.CodigoVerificacion).HasMaxLength(250);
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.EstaVerificado).HasDefaultValue(false);
            entity.Property(e => e.FechaVerificacion).HasColumnType("datetime");
            entity.Property(e => e.HorariosPrivados).HasDefaultValue(false);
            entity.Property(e => e.IdAvatar).HasDefaultValue(1);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Password_Hash).HasMaxLength(150);
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

           
        });

        modelBuilder.Entity<Usuario_Grupo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuario___3214EC071B8B7C79");

            entity.HasIndex(e => new { e.IdUsuario, e.IdGrupo }, "UQ_Usuario_Grupo").IsUnique();

            entity.Property(e => e.Ingreso)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.Usuario_Grupo)
                .HasForeignKey(d => d.IdGrupo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Grupo_Grupo");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Usuario_Grupo)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Grupo_Usuario");
        });

        modelBuilder.Entity<Usuario_Plan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuario___3214EC079B188845");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Plan).WithMany(p => p.Usuario_Plan)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario_P__PlanI__60A75C0F");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Usuario_Plan)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario_P__Usuar__5FB337D6");
        });

        modelBuilder.Entity<Usuario_Preferencia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuario___3214EC07E3AB4BA4");

            entity.HasIndex(e => new { e.IdUsuario, e.IdPreferencia }, "UQ_Usuario_Preferencia").IsUnique();

            entity.HasOne(d => d.IdPreferenciaNavigation).WithMany(p => p.Usuario_Preferencia)
                .HasForeignKey(d => d.IdPreferencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Preferencia_Preferencia");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Usuario_Preferencia)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Preferencia_Usuario");
        });

        modelBuilder.Entity<Voto_Plan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Voto_Pla__3214EC071372FDA1");

            entity.Property(e => e.FechaVoto)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Plan).WithMany(p => p.Voto_Plan)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Voto_Plan__PlanI__693CA210");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Voto_Plan)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Voto_Plan__Usuar__68487DD7");
        });

        modelBuilder.Entity<Zonas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Zonas__3214EC0726A358FC");

            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Zonas_Grupos>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Zonas_Gr__3214EC070EFF75B2");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.Zonas_Grupos)
                .HasForeignKey(d => d.IdGrupo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Zonas_Grupos_Grupo");

            entity.HasOne(d => d.IdZonaNavigation).WithMany(p => p.Zonas_Grupos)
                .HasForeignKey(d => d.IdZona)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Zonas_Grupos_Zonas");
        });

        modelBuilder.Entity<Usuario_Propuesta>(entity =>
        {
            entity.HasKey(e => new { e.IdUsuario, e.IdPropuesta }).HasName("PK__Usuario___FAF78078F3412204");
            entity.ToTable("Usuario_Propuesta");

            entity.HasOne(d => d.Usuario)
      .WithMany(p => p.Usuario_Propuesta)
      .HasForeignKey(d => d.IdUsuario)
      .OnDelete(DeleteBehavior.ClientSetNull)
      .HasConstraintName("FK_Usuario_Propuesta_Usuario");

            entity.HasOne(d => d.Propuesta)
                .WithMany(p => p.Usuario_Propuesta)
                .HasForeignKey(d => d.IdPropuesta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Propuesta_Propuesta");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
