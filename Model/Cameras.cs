namespace Camera.Model
{
    public class CameraInfo
    {
        public int Id { get; set; }                     
        public string City { get; set; }               
        public string Street { get; set; }              
        public DateTime UpdatedAt { get; set; }           
        public string IpAddress { get; set; }           
        public string Port { get; set; }                
        public string PositionLat { get; set; }         
        public string PositionLong { get; set; }        
        public double TiltAngle { get; set; }           
        public double PanAngle { get; set; }            
    }
}
