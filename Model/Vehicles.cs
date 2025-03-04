namespace Camera.Model
{
    public class VehiclesInfo
    {
        public int Id { get; set; }
        public int CameraId { get; set; }
        public DateTime CreateAt { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public int Type { get; set; }  // 1 = Car, 2 = Bicycle
    }
}
