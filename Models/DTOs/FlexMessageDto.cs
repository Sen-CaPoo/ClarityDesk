namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// LINE Flex Message DTO
    /// 用於構建 Flex Message 格式的推送訊息
    /// </summary>
    public class FlexMessageDto
    {
        public string Type { get; set; } = "flex";
        public string AltText { get; set; } = string.Empty;
        public FlexContainerDto? Contents { get; set; }
    }

    /// <summary>
    /// Flex Message 容器
    /// </summary>
    public class FlexContainerDto
    {
        public string Type { get; set; } = "bubble";
        public FlexBoxDto? Header { get; set; }
        public FlexBoxDto? Body { get; set; }
        public FlexBoxDto? Footer { get; set; }
    }

    /// <summary>
    /// Flex Box 元件
    /// </summary>
    public class FlexBoxDto
    {
        public string Type { get; set; } = "box";
        public string Layout { get; set; } = "vertical";
        public List<object> Contents { get; set; } = new();
        public string? Spacing { get; set; }
        public string? Margin { get; set; }
    }

    /// <summary>
    /// Flex Text 元件
    /// </summary>
    public class FlexTextDto
    {
        public string Type { get; set; } = "text";
        public string Text { get; set; } = string.Empty;
        public string? Size { get; set; }
        public string? Weight { get; set; }
        public string? Color { get; set; }
        public bool? Wrap { get; set; }
        public int? Flex { get; set; }
    }

    /// <summary>
    /// Flex Button 元件
    /// </summary>
    public class FlexButtonDto
    {
        public string Type { get; set; } = "button";
        public FlexActionDto? Action { get; set; }
        public string? Style { get; set; }
    }

    /// <summary>
    /// Flex Action
    /// </summary>
    public class FlexActionDto
    {
        public string Type { get; set; } = "uri";
        public string Label { get; set; } = string.Empty;
        public string? Uri { get; set; }
        public string? Data { get; set; }
    }

    /// <summary>
    /// Flex Separator 元件
    /// </summary>
    public class FlexSeparatorDto
    {
        public string Type { get; set; } = "separator";
        public string? Margin { get; set; }
    }
}
