using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PapatzoaPOS.Modelos
{
    // COMPOSICIÓN: Una Venta se compone de una lista de DetalleVenta y un Cliente opcional
    public class Venta
    {
        private readonly List<DetalleVenta> detalles = new List<DetalleVenta>();
        private readonly bool esHistorico;

        // Campos de respaldo cuando la venta se carga desde el historial CSV
        private readonly double subtotalHistorico;
        private readonly double descuentoComboHistorico;
        private readonly double descuentoCortesiaHistorico;
        private readonly double totalHistorico;
        private readonly bool aplicoComboHistorico;
        private readonly string resumenHistorico = string.Empty;

        public string Folio { get; private set; } = string.Empty;
        public DateTime Fecha { get; private set; }
        public Cliente? ClienteAsociado { get; private set; }
        public string NombreCliente { get; private set; } = "Público General";
        public double PagoRecibido { get; private set; }
        public bool AplicoCortesia { get; private set; }

        // Constructor para una nueva venta en caja
        public Venta(string folio, Cliente? cliente = null)
        {
            if (string.IsNullOrWhiteSpace(folio))
                throw new ArgumentException("El folio de la venta es obligatorio.");

            Folio = folio.Trim().ToUpper();
            Fecha = DateTime.Now;
            AsignarCliente(cliente);
            esHistorico = false;
        }

        // Constructor para reconstruir ventas guardadas en archivo CSV
        public Venta(string folio, DateTime fecha, string nombreCliente, double subtotal,
                     double descuentoCombo, double descuentoCortesia, double total,
                     double pagoRecibido, bool aplicoCombo, bool aplicoCortesia, string resumenArticulos)
        {
            Folio = folio;
            Fecha = fecha;
            NombreCliente = string.IsNullOrWhiteSpace(nombreCliente) ? "Público General" : nombreCliente;
            subtotalHistorico = subtotal;
            descuentoComboHistorico = descuentoCombo;
            descuentoCortesiaHistorico = descuentoCortesia;
            totalHistorico = total;
            PagoRecibido = pagoRecibido;
            aplicoComboHistorico = aplicoCombo;
            AplicoCortesia = aplicoCortesia;
            resumenHistorico = resumenArticulos;
            esHistorico = true;
        }

        public IReadOnlyList<DetalleVenta> Detalles => detalles.AsReadOnly();

        public void AsignarCliente(Cliente? cliente)
        {
            ClienteAsociado = cliente;
            NombreCliente = cliente != null ? cliente.Nombre : "Público General";
            // Si cambia de cliente, reiniciamos la cortesía por seguridad
            AplicoCortesia = false;
        }

        public void AgregarDetalle(DetalleVenta detalle)
        {
            if (detalle == null)
                throw new ArgumentNullException(nameof(detalle));

            detalles.Add(detalle);
        }

        public void EliminarDetalle(int indice)
        {
            if (indice >= 0 && indice < detalles.Count)
            {
                detalles.RemoveAt(indice);
                if (detalles.Count == 0)
                    AplicoCortesia = false;
            }
        }

        public void VaciarCarrito()
        {
            detalles.Clear();
            AplicoCortesia = false;
            PagoRecibido = 0;
        }

        // Activa la cortesía si el cliente tiene 5 sellos (descuenta 1 pieza del artículo más económico)
        public bool SolicitarCortesiaCliente()
        {
            if (ClienteAsociado == null || !ClienteAsociado.TieneCortesiaDisponible() || detalles.Count == 0)
                return false;

            AplicoCortesia = true;
            return true;
        }

        // Regla de oro Papatzoa: 10% de descuento automático al combinar Bebida + Panqué o Pan Tradicional
        public bool AplicaComboApapacho
        {
            get
            {
                if (esHistorico) return aplicoComboHistorico;

                bool tieneBebida = detalles.Any(d => d.Categoria.Equals("Bebida", StringComparison.OrdinalIgnoreCase));
                bool tienePan = detalles.Any(d =>
                    d.Categoria.Equals("Panqué", StringComparison.OrdinalIgnoreCase) ||
                    d.Categoria.Equals("Pan Tradicional", StringComparison.OrdinalIgnoreCase));

                return tieneBebida && tienePan;
            }
        }

        public double Subtotal => esHistorico
            ? subtotalHistorico
            : Math.Round(detalles.Sum(d => d.Subtotal), 2);

        public double DescuentoCombo => esHistorico
            ? descuentoComboHistorico
            : (AplicaComboApapacho ? Math.Round(Subtotal * 0.10, 2) : 0.0);

        public double DescuentoCortesia => esHistorico
            ? descuentoCortesiaHistorico
            : (AplicoCortesia && detalles.Count > 0 ? Math.Round(detalles.Min(d => d.PrecioUnitario), 2) : 0.0);

        public double Total
        {
            get
            {
                if (esHistorico) return totalHistorico;
                double calculo = Subtotal - DescuentoCombo - DescuentoCortesia;
                return Math.Round(Math.Max(0, calculo), 2);
            }
        }

        public double Cambio => Math.Round(Math.Max(0, PagoRecibido - Total), 2);

        public string ResumenArticulos
        {
            get
            {
                if (esHistorico) return resumenHistorico;
                if (detalles.Count == 0) return "Sin artículos";
                return string.Join(" | ", detalles.Select(d => $"{d.Cantidad}x {d.NombreProducto}"));
            }
        }

        public void FinalizarCobro(double montoRecibido)
        {
            if (detalles.Count == 0)
                throw new InvalidOperationException("Agrega al menos un producto antes de cobrar.");

            if (Math.Round(montoRecibido, 2) < Total)
                throw new ArgumentException($"El pago recibido (${montoRecibido:F2}) es menor al total (${Total:F2}).");

            PagoRecibido = Math.Round(montoRecibido, 2);
            Fecha = DateTime.Now;

            if (ClienteAsociado != null)
            {
                if (AplicoCortesia)
                    ClienteAsociado.CanjearCortesia();
                else
                    ClienteAsociado.AgregarSello();
            }
        }

        public string GenerarTicketTexto()
        {
            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("               PAPATZOA                   ");
            sb.AppendLine("      Sabores que apapachan el alma       ");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Folio:   {Folio}");
            sb.AppendLine($"Fecha:   {Fecha:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Cliente: {NombreCliente}");
            sb.AppendLine("------------------------------------------");

            if (detalles.Count > 0)
            {
                foreach (var item in detalles)
                {
                    sb.AppendLine(item.ObtenerLineaTicket());
                }
            }
            else
            {
                sb.AppendLine(ResumenArticulos);
            }

            sb.AppendLine("------------------------------------------");
            sb.AppendLine($"Subtotal:            ${Subtotal,10:F2}");

            if (DescuentoCombo > 0)
                sb.AppendLine($"Combo Apapacho (-10%):-${DescuentoCombo,9:F2}");

            if (DescuentoCortesia > 0)
                sb.AppendLine($"Cortesía 5 Sellos:   -${DescuentoCortesia,9:F2}");

            sb.AppendLine($"TOTAL A PAGAR:       ${Total,10:F2}");
            sb.AppendLine($"Pago recibido:       ${PagoRecibido,10:F2}");
            sb.AppendLine($"Cambio:              ${Cambio,10:F2}");
            sb.AppendLine("==========================================");

            if (ClienteAsociado != null)
            {
                sb.AppendLine($"Sellos acumulados: {ClienteAsociado.SellosAcumulados}/5");
                if (ClienteAsociado.TieneCortesiaDisponible())
                    sb.AppendLine("¡Ya tienes una cortesía para tu próxima visita!");
            }

            sb.AppendLine("    ¡Gracias por acompañarnos con pan     ");
            sb.AppendLine("           y café tradicional!            ");
            sb.AppendLine("==========================================");
            return sb.ToString();
        }

        public string GuardarTicketArchivo(string carpetaTickets)
        {
            Directory.CreateDirectory(carpetaTickets);
            string nombreArchivo = $"Ticket_{Folio}_{Fecha:yyyyMMdd_HHmmss}.txt";
            string rutaCompleta = Path.Combine(carpetaTickets, nombreArchivo);
            File.WriteAllText(rutaCompleta, GenerarTicketTexto(), Encoding.UTF8);
            return rutaCompleta;
        }
    }
}