using System;

namespace PapatzoaPOS.Modelos
{
    public class PanqueArtesanal : Producto
    {
        private string sabor = string.Empty;
        private string presentacion = string.Empty;
        public bool ServirTibio { get; set; }
        public bool ConExtraNata { get; set; }

        public PanqueArtesanal(string codigo, string nombre, double precioBase, int stock,
                               string sabor, string presentacion = "Rebanada",
                               bool servirTibio = true, bool conExtraNata = false, int stockMinimo = 5)
            : base(codigo, nombre, precioBase, stock, stockMinimo)
        {
            Sabor = sabor;
            Presentacion = presentacion;
            ServirTibio = servirTibio;
            ConExtraNata = conExtraNata;
        }

        public string Sabor
        {
            get { return sabor; }
            set { sabor = string.IsNullOrWhiteSpace(value) ? "Receta de la casa" : value.Trim(); }
        }

        public string Presentacion
        {
            get { return presentacion; }
            set { presentacion = string.IsNullOrWhiteSpace(value) ? "Rebanada" : value.Trim(); }
        }

        public override string Categoria => "Panqué";

        // Comportamiento polimórfico: si pide acompañamiento de nata artesanal suma $10
        public override double CalcularPrecioFinal()
        {
            double extra = ConExtraNata ? 10.0 : 0.0;
            return Math.Round(PrecioBase + extra, 2);
        }

        public override string ObtenerDetallesEspecificos()
        {
            string temp = ServirTibio ? "Tibio" : "Ambiente";
            string nata = ConExtraNata ? " + Extra Nata" : "";
            return $"Sabor {Sabor} | {Presentacion} | {temp}{nata}";
        }
    }
}