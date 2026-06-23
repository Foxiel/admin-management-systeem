using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models
{
    public class SendOrder
    {
        public int BestellingId { get; set; }

        public int KlantId { get; set; }
        public string KlantNaam { get; set; } = string.Empty;
        public string KlantEmail { get; set; } = string.Empty;
        public string KlantTelefoon { get; set; } = string.Empty;

        public string Adres { get; set; } = string.Empty;
        public string Postcode { get; set; } = string.Empty;
        public string Woonplaats { get; set; } = string.Empty;
        public string Land { get; set; } = string.Empty;

        public DateTime OrderDatum { get; set; }
        public string OrderStatus { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kies een bezorger.")]
        public int BezorgerId { get; set; }

        [Required(ErrorMessage = "Vul een leverdatum in.")]
        public DateTime LeverDatum { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vul een levertijd in.")]
        public TimeSpan LeverTijd { get; set; }

        public int Status { get; set; } = 1;

        [Required(ErrorMessage = "Vul een track & trace code in.")]
        public string TrackTraceCode { get; set; } = string.Empty;

        public List<BezorgerOption> Bezorgers { get; set; } = new();
        public List<BezorgStatusOption> Statussen { get; set; } = new();
    }

    public class BezorgerOption
    {
        public int Id { get; set; }
        public string Naam { get; set; } = string.Empty;
    }

    public class BezorgStatusOption
    {
        public int Id { get; set; }
        public string Naam { get; set; } = string.Empty;
    }
}