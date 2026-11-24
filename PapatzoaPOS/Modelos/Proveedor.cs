using System;

namespace PapatzoaPOS.Modelos
{
    public class Proveedor : Persona
    {
        private string empresa = string.Empty;
        private string rfc = string.Empty;
        private string categoriaInsumo = string.Empty;

        public Proveedor(string id, string nombreContacto, string telefono, string correo,
                         string empresa, string rfc, string categoriaInsumo)
            : base(id, nombreContacto, telefono, correo)
        {
            Empresa = empresa;
            Rfc = rfc;
            CategoriaInsumo = categoriaInsumo;
        }

        public string Empresa
        {
            get { return empresa; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El nombre de la empresa proveedora es obligatorio.");
                empresa = value.Trim();
            }
        }

        public string Rfc
        {
            get { return rfc; }
            set { rfc = string.IsNullOrWhiteSpace(value) ? "XAXX010101000" : value.Trim().ToUpper(); }
        }

        public string CategoriaInsumo
        {
            get { return categoriaInsumo; }
            set { categoriaInsumo = string.IsNullOrWhiteSpace(value) ? "Insumos Generales" : value.Trim(); }
        }

        public override string ObtenerResumen()
        {
            return $"Proveedor: {Empresa} (Contacto: {Nombre}) | Insumo: {CategoriaInsumo} | RFC: {Rfc}";
        }
    }
}