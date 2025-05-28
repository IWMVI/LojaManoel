using System.ComponentModel.DataAnnotations;

namespace LojaManoel.Models
{
    public class ProdutoInput
    {
        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome do produto deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;
        [Range(0.1, double.MaxValue, ErrorMessage = "A altura deve ser maior que zero.")]
        public decimal Altura { get; set; }
        [Range(0.1, double.MaxValue, ErrorMessage = "A largura deve ser maior que zero.")]
        public decimal Largura { get; set; }
        [Range(0.1, double.MaxValue, ErrorMessage = "O comprimento deve ser maior que zero.")]
        public decimal Comprimento { get; set; }
    }
}
