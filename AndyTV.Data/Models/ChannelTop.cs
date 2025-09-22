using System;
using System.Collections.Generic;
using System.Text;

namespace AndyTV.Data.Models;

public class ChannelTop
{
    public string Name { get; set; }
    public List<string> AltNames { get; set; }
    public string StreamingTvId { get; set; }
}