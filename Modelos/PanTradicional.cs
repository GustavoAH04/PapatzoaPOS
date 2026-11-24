using System;

namespace PapatzoaPOS.Modelos
{
    public class PanTradicional : Producto
    {
        private string tipoMasa = string.Empty;
        public bool RecienHorneado { get; set; }

        public PanTradicional(string codigo, string nombre, double precioBase, int stock,
                              string tipoMasa, bool recienHorneado = true, int stockMinimo = 6)
            : base(codigo, nombre, precioBase, stock, stockMinimo)
        {
            TipoMasa = tipoMasa;
            RecienHorneado = recienHorneado;
        }

        public string TipoMasa
        {
            get { return tipoMasa; }
            set { tipoMasa = string.IsNullOrWhiteSpace(value) ? "Masa dulce artesanal" : value.Trim(); }
        }

        public override string Categoria => "Pan Tradicional";

        // El pan tradicional mantiene su precio fijo artesanal de panadería
        public override double CalcularPrecioFinal()
        {
            return Math.Round(PrecioBase, 2);
        }

        public override string ObtenerDetallesEspecificos()
        {
            string frescura = RecienHorneado ? "Recién horneado" : "Horneado del día";
            return $"Masa: {TipoMasa} | {frescura}";
        }
    }
}