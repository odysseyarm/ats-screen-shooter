using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ATSCfg : MonoBehaviour
{
    public MarkersSettings settings = new MarkersSettings();

    // Start is called before the first frame update
    void Start()
    {
        StreamReader reader = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/odysseyarm/odyssey/config/markers.toml");
        settings = Tomlyn.Toml.ToModel<MarkersSettings>(reader.ReadToEnd());
    }
}

public class MarkerPosition
{
    public int x { get; set; }
    public int y { get; set; }
}

public class Marker
{
    public MarkerPosition position { get; set; }
}

// the markers are positioned in a cross pattern around the center of the view
public class MarkerView
{
    public Marker marker_top { get; set; }
    public Marker marker_right { get; set; }
    public Marker marker_bottom { get; set; }
    public Marker marker_left { get; set; }
}

public class MarkersSettings
{
    public MarkersSettings() {
        views = new List<MarkerView>();
    }

    public IList<MarkerView> views { get; }
}
