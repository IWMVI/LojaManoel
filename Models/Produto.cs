namespace LojaManoel.Models
{
    public class Produto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Altura { get; set; }
        public decimal Largura { get; set; }
        public decimal Comprimento { get; set; }
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }
        public int? CaixaId { get; set; }
        public int? Caixa { get; set; }

    }
}
