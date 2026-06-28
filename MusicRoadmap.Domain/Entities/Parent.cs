using System;
using System.Collections.Generic;

namespace MusicRoadmap.Domain.Entities;

public class Parent
{
    public string Id {get; set;} = Guid.NewGuid().ToString();
    public string InstructorId {get; set;} = string.Empty;
    public string FirstName {get; set;} = string.Empty;
    public string LastName {get; set;} = string.Empty;
    public string Email {get; set;} = string.Empty;
    public string Phone {get; set;} = string.Empty;
    public string Status {get; set;} = "Pending";
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public Instructor Instructor {get; set;}= null!;
    public ICollection<Student> Students = new List<Student>();
}