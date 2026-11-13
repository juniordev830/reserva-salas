using ReservaSalas;
namespace ReservaSalas.UnitTests;
public class ReservaTests {
 [Fact] public void RejectsEmptyResource() => Assert.Throws<ArgumentException>(() => Reserva.Criar(1," ",DateTime.UtcNow,DateTime.UtcNow.AddHours(1)));
 [Fact] public void RejectsInvalidId() => Assert.Throws<ArgumentOutOfRangeException>(() => Reserva.Criar(0,"A",DateTime.UtcNow,DateTime.UtcNow.AddHours(1)));
 [Fact] public void RejectsReversedPeriod(){var now=DateTime.UtcNow;Assert.Throws<ArgumentException>(()=>Reserva.Criar(1,"A",now,now));}
 [Fact] public void RejectsOverlappingBooking(){var s=new ReservaService();var now=DateTime.UtcNow;s.Reservar("A",now,now.AddHours(2));Assert.Throws<InvalidOperationException>(()=>s.Reservar("a",now.AddHours(1),now.AddHours(3)));}
 [Fact] public void AllowsAdjacentBooking(){var s=new ReservaService();var now=DateTime.UtcNow;s.Reservar("A",now,now.AddHours(1));s.Reservar("A",now.AddHours(1),now.AddHours(2));Assert.Equal(2,s.Listar().Count);}
 [Fact] public void AllowsDifferentRooms(){var s=new ReservaService();var now=DateTime.UtcNow;s.Reservar("A",now,now.AddHours(1));s.Reservar("B",now,now.AddHours(1));Assert.Equal(2,s.Listar().Count);}
 [Fact] public void CancelsOnlyRequestedBooking(){var s=new ReservaService();var now=DateTime.UtcNow;var a=s.Reservar("A",now,now.AddHours(1));s.Reservar("B",now,now.AddHours(1));s.Cancelar(a.Id);Assert.Single(s.Listar());}
 [Fact] public void RejectsUnknownCancellation(){var s=new ReservaService();Assert.Throws<KeyNotFoundException>(()=>s.Cancelar(99));}

 [Fact] public void ReturnsSnapshotNotMutableBackingList(){
   var s=new ReservaService();var now=DateTime.UtcNow;
   var before=s.Listar();
   s.Reservar("A",now,now.AddHours(1));
   Assert.Empty(before);
   Assert.Single(s.Listar());
 }
 [Fact] public async Task ConcurrentBookingsCannotOverlap(){
   var s=new ReservaService();var now=DateTime.UtcNow;
   var attempts=Enumerable.Range(0,20).Select(_=>Task.Run(()=>{
     try {s.Reservar("A",now,now.AddHours(1));return true;}
     catch(InvalidOperationException){return false;}
   }));
   var results=await Task.WhenAll(attempts);
   Assert.Single(results.Where(x=>x));
   Assert.Single(s.Listar());
 }

 [Fact] public void RejectsNegativeId(){var now=DateTime.UtcNow;Assert.Throws<ArgumentOutOfRangeException>(()=>Reserva.Criar(-1,"A",now,now.AddMinutes(5)));}
 [Fact] public void TrimsResourceName(){var now=DateTime.UtcNow;Assert.Equal("Sala A",Reserva.Criar(1," Sala A ",now,now.AddMinutes(5)).Recurso);}
 [Fact] public void RejectsSameStartTime(){var s=new ReservaService();var now=DateTime.UtcNow;s.Reservar("A",now,now.AddMinutes(30));Assert.Throws<InvalidOperationException>(()=>s.Reservar("A",now,now.AddMinutes(10)));}
 [Fact] public void AllowsBookingAfterCancellation(){var s=new ReservaService();var now=DateTime.UtcNow;var booking=s.Reservar("A",now,now.AddMinutes(30));s.Cancelar(booking.Id);Assert.NotNull(s.Reservar("A",now,now.AddMinutes(30)));}
 [Fact] public void RejectsDoubleCancellation(){var s=new ReservaService();var now=DateTime.UtcNow;var booking=s.Reservar("A",now,now.AddMinutes(30));s.Cancelar(booking.Id);Assert.Throws<KeyNotFoundException>(()=>s.Cancelar(booking.Id));}
}
