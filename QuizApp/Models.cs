using System;
using System.Collections.Generic;

public class User
{
    public string Login { get; set; }
    public string Password { get; set; }
    public DateTime BirthDate { get; set; }
}

public class Answer
{
    public string Text { get; set; }
    public bool IsCorrect { get; set; }
}

public class Question
{
    public string Text { get; set; }
    public List<Answer> Answers { get; set; } = new List<Answer>();
}

public class Quiz
{
    public string Title { get; set; }
    public string Category { get; set; }
    public List<Question> Questions { get; set; } = new List<Question>();
}

public class QuizResult
{
    public string UserLogin { get; set; }
    public string QuizTitle { get; set; }
    public int Score { get; set; }
    public DateTime Date { get; set; }
}

