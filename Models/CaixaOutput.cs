namespace LojaManoel.Models
{
    public class CaixaOutput
    {
        public string Nome { get; set; } = string.Empty;
        public List<String> Produtos { get; set; } = new();
    }
}
