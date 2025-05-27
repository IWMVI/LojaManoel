namespace LojaManoel.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public List<Produto> Produtos { get; set; } = new();
        public List<CaixaSelecionada> Caixas { get; set; } = new();
    }
}
