using Food3DModel.Model;

namespace Food3DModel.Interface
{
    public interface IFood3DModelAPIHandler
    {
        Food3DModelAPIBody Request(string foodName);
    }
}