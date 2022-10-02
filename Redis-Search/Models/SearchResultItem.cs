using System;
namespace Redis_Search.Models
{
    public record SearchResultItem(
        string Vin,
        string Year,
        string Make,
        string Model,
        string Trim,
        int Mileage,
        string Body,
        int Doors,
        string Drivetrain,
        string Transmission,
        string Interior_color,
        string Exterior_color,
        string Engine
    );
}

