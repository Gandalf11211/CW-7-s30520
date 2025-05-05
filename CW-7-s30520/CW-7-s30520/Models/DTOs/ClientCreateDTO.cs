using System.ComponentModel.DataAnnotations;

namespace CW_7_s30520.Models.DTOs;

public class ClientCreateDTO
{
    [Length(1, 30)]
    public required string FirstName { get; set; }
    [Length(1, 40)]
    public required string LastName { get; set; }
    [Length(1, 120)]
    public required string Email { get; set; }
    [Length(1, 25)]
    public string Telephone { get; set; }
    [Length(1, 11)]
    public string Pesel { get; set; }
}