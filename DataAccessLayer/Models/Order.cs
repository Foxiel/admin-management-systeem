//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Order
{
    [Key]
    public int Id { get; set; }

    public Customer? Klant { get; set; }

    // bewaar voor compatibiliteit (kan leeg zijn als we Bestelregels gebruiken)
    public List<Product>? Producten { get; set; }

    // bestellijnen met aantallen
    public List<OrderItem>? Bestelregels { get; set; }

    public DateTime BestelDatum { get; set; }
    public string BestelStatus { get; set; } = string.Empty;
}
