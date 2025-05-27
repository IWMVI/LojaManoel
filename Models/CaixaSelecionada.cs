namespace LojaManoel.Models
{
    public class CaixaSelecionada
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public List<Produto> Produtos { get; set; } = new();
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }
    }
}
