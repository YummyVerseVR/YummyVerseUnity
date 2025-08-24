namespace Food3DModel.Model
{
    public class Food3DModelAPIBody
    {
        public byte[] GlbData;
        
        public Food3DModelAPIBody(byte[] glbData){
            GlbData = glbData;
        }
    }
}