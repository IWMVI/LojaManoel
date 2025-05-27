namespace LojaManoel.Models
{
    public class Caixa
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Altura { get; set; }
        public decimal Largura { get; set; }
        public decimal Comprimento { get; set; }
        public List<Produto> Produtos { get; set; } = new();

    }
}
