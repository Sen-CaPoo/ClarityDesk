namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// LINE 綁定 DTO
    /// </summary>
    public class LineBindingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string LineUserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime BoundAt { get; set; }
        public DateTime? UnboundAt { get; set; }
    }
}
