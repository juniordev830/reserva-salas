using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReservaSalas.Api;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default required");
var secret = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key required (minimum 32 characters)");
if (Encoding.UTF8.GetByteCount(secret) < 32) throw new InvalidOperationException("Jwt:Key must be at least 32 bytes");
builder.Services.AddDbContext<BookingDb>(o => o.UseNpgsql(connection));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => {
    o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "reserva-salas",
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"] ?? "reserva-salas",
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization(o => o.AddPolicy("Admin", p => p.RequireRole("admin")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("frontend", p => p.WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("frontend"); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
var api = app.MapGroup("/api").RequireAuthorization();
api.MapGet("/rooms", async (BookingDb db, CancellationToken ct) =>
    Results.Ok(await db.Rooms.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct)));
api.MapPost("/rooms", async (RoomInput input, BookingDb db, CancellationToken ct) => {
    if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 120 || input.Capacity is < 1 or > 10000) return Results.BadRequest(new { error = "Invalid room" });
    var room = new Room { Name = input.Name.Trim(), Capacity = input.Capacity, Active = true };
    if (await db.Rooms.AnyAsync(r => r.Name == room.Name, ct)) return Results.Conflict(new { error = "Room name already exists" });
    db.Rooms.Add(room);
    try { await db.SaveChangesAsync(ct); }
    catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { return Results.Conflict(new { error = "Room name already exists" }); }
    return Results.Created($"/api/rooms/{room.Id}", room);
}).RequireAuthorization("Admin");
api.MapGet("/bookings", async (int? roomId, BookingDb db, CancellationToken ct) =>
    Results.Ok(await db.Bookings.AsNoTracking().Where(b => !roomId.HasValue || b.RoomId == roomId).OrderBy(b => b.StartsAt).Take(500).ToListAsync(ct)));
api.MapPost("/bookings", async (BookingInput input, BookingDb db, ClaimsPrincipal user, CancellationToken ct) => {
    if (input.RoomId <= 0 || input.StartsAt.Offset != TimeSpan.Zero || input.EndsAt.Offset != TimeSpan.Zero || input.EndsAt <= input.StartsAt || input.StartsAt == default || input.EndsAt == default)
        return Results.BadRequest(new { error = "Use UTC timestamps with end after start" });
    var actor = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (string.IsNullOrWhiteSpace(actor)) return Results.Forbid();
    await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
    try {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == input.RoomId && r.Active, ct);
        if (room is null) return Results.NotFound(new { error = "Room not found" });
        var conflict = await db.Bookings.AnyAsync(b => b.RoomId == input.RoomId && b.StartsAt < input.EndsAt && b.EndsAt > input.StartsAt, ct);
        if (conflict) return Results.Conflict(new { error = "Room already booked" });
        var booking = new Booking { RoomId = input.RoomId, StartsAt = input.StartsAt, EndsAt = input.EndsAt, OwnerId = actor };
        db.Bookings.Add(booking); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Results.Created($"/api/bookings/{booking.Id}", booking);
    } catch (Npgsql.PostgresException e) when (e.SqlState is "40001" or "23P01") {
        return Results.Conflict(new { error = "Concurrent booking; retry" });
    }
});
api.MapDelete("/bookings/{id:int}", async (int id, BookingDb db, ClaimsPrincipal user, CancellationToken ct) => {
    var booking = await db.Bookings.FindAsync(new object[] { id }, ct);
    if (booking is null) return Results.NotFound();
    var actor = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (booking.OwnerId != actor && !user.IsInRole("admin")) return Results.Forbid();
    db.Bookings.Remove(booking); await db.SaveChangesAsync(ct);
    return Results.NoContent();
});
app.Run();
public partial class Program { }
