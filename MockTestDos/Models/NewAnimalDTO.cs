﻿namespace MockTestDos.Models;

public class NewAnimalDTO
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public int OwnerId { get; set; }
    public List<ProcedureWithDate> Procedures { get; set; } = new List<ProcedureWithDate>();
}

public class ProcedureWithDate
{
    public int ProcedureId { get; set; }
    public DateTime Date { get; set; }
}