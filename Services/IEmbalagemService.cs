using LojaManoel.Models;

namespace LojaManoel.Services
{
    public interface IEmbalagemService
    {
        Task<EmbalagemOutput> ProcessarEmbalagem(EmbalagemInput input);
    }
}
