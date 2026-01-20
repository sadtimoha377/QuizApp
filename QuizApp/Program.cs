using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static string usersFile = "Data/users.json";
    static string resultsFile = "Data/results.json";

    static List<User> users;
    static List<QuizResult> results;

    static void Main()
    {
        Directory.CreateDirectory("Data");

        users = DataService.Load<User>(usersFile);
        results = DataService.Load<QuizResult>(resultsFile);

        while (true)
        {
            Console.WriteLine("\n=== Quiz App ===");
            Console.WriteLine("1 - Log In");
            Console.WriteLine("2 - Sign Up");
            Console.WriteLine("0 - Exit");
            Console.Write("Your choice: ");

            var choice = Console.ReadLine();
            if (choice == "0") break;

            if (choice == "2") Register();
            else if (choice == "1") Login();
            else Console.WriteLine("Invalid option!");
        }
    }
    static void Register()
    {
        Console.Write("Login: ");
        var login = Console.ReadLine();

        if (users.Any(u => u.Login == login))
        {
            Console.WriteLine("Login already exists!");
            return;
        }

        Console.Write("Password: ");
        var password = Console.ReadLine();

        DateTime birthDate;
        while (true)
        {
            Console.Write("Birth date (formate is yrs-m-d): ");
            if (DateTime.TryParse(Console.ReadLine(), out birthDate)) break;
            Console.WriteLine("Invalid date format!");
        }

        users.Add(new User { Login = login, Password = password, BirthDate = birthDate });
        DataService.Save(usersFile, users);
        Console.WriteLine("Registration completed!");
    }
    static void Login()
    {
        Console.Write("Login: ");
        var login = Console.ReadLine();
        Console.Write("Password: ");
        var password = Console.ReadLine();

        var user = users.FirstOrDefault(u => u.Login == login && u.Password == password);
        if (user == null)
        {
            Console.WriteLine("Wrong login or password!");
            return;
        }

        Console.WriteLine($"Welcome, {user.Login}!");
        UserMenu(user);
    }
    static void UserMenu(User user)
    {
        while (true)
        {
            Console.WriteLine("\n=== Menu ===");
            Console.WriteLine("1 - Start New Quiz");
            Console.WriteLine("2 - My Results");
            Console.WriteLine("3 - Top-20");
            Console.WriteLine("4 - Account Settings");
            Console.WriteLine("0 - Logout");
            Console.Write("Your choice: ");

            var choice = Console.ReadLine();
            if (choice == "0") break;

            switch (choice)
            {
                case "1": StartQuiz(user); break;
                case "2": ShowUserResults(user); break;
                case "3": ShowTopResults(); break;
                case "4": EditUserSettings(user); break;
                default: Console.WriteLine("Invalid option!"); break;
            }
        }
    }
    static void StartQuiz(User user)
    {
        Console.WriteLine("\nChoose difficulty:");
        Console.WriteLine("1 - Easy");
        Console.WriteLine("2 - Medium");
        Console.WriteLine("3 - Hard");

        int difficulty = 0;
        while (!int.TryParse(Console.ReadLine(), out difficulty) || difficulty < 1 || difficulty > 3)
            Console.WriteLine("Invalid option! Try again.");

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = difficulty switch
        {
            1 => Path.Combine(baseDir, "Data", "easy.txt"),
            2 => Path.Combine(baseDir, "Data", "medium.txt"),
            3 => Path.Combine(baseDir, "Data", "hard.txt"),
            _ => Path.Combine(baseDir, "Data", "easy.txt")
        };

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File {filePath} not found!");
            return;
        }

        List<Question> selectedQuestions = LoadQuestionsFromFile(filePath);

        selectedQuestions = selectedQuestions.OrderBy(q => Guid.NewGuid())
                                             .Take(20)
                                             .ToList();

        int score = 0;
        foreach (var q in selectedQuestions)
        {
            Console.WriteLine($"\n{q.Text}");
            for (int i = 0; i < q.Answers.Count; i++)
                Console.WriteLine($"{i + 1}. {q.Answers[i].Text}");

            List<int> input;
            while (true)
            {
                try
                {
                    input = Console.ReadLine()
                        .Split(',')
                        .Select(x => int.Parse(x.Trim()) - 1)
                        .OrderBy(x => x)
                        .ToList();
                    if (input.All(i => i >= 0 && i < q.Answers.Count)) break;
                    else Console.WriteLine("Invalid answer numbers! Try again.");
                }
                catch
                {
                    Console.WriteLine("Invalid format! Separate numbers by commas.");
                }
            }

            var correct = q.Answers.Select((a, i) => a.IsCorrect ? i : -1)
                                    .Where(i => i != -1)
                                    .OrderBy(i => i)
                                    .ToList();

            if (input.SequenceEqual(correct)) score++;
        }

        results.Add(new QuizResult
        {
            UserLogin = user.Login,
            QuizTitle = $"Quiz {difficulty}",
            Score = score,
            Date = DateTime.Now
        });
        DataService.Save(resultsFile, results);

        Console.WriteLine($"\nYou scored {score}/{selectedQuestions.Count}!");
    }
    static List<Question> LoadQuestionsFromFile(string path)
    {
        var questions = new List<Question>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(' ');
            if (parts.Length < 2) continue;

            var q = new Question();
            q.Text = string.Join(" ", parts.Take(parts.Length - 4));

            var answers = parts.Skip(parts.Length - 4).ToList();
            foreach (var a in answers)
            {
                if (string.IsNullOrEmpty(a)) continue;
                bool correct = a.EndsWith("*");
                string text = correct ? a.TrimEnd('*') : a;
                q.Answers.Add(new Answer { Text = text, IsCorrect = correct });
            }

            q.Answers = q.Answers.OrderBy(x => Guid.NewGuid()).ToList();
            questions.Add(q);
        }

        return questions;
    }
    static void ShowUserResults(User user)
    {
        var userResults = results.Where(r => r.UserLogin == user.Login)
                                 .OrderByDescending(r => r.Date)
                                 .ToList();
        if (!userResults.Any())
        {
            Console.WriteLine("No results yet.");
            return;
        }

        Console.WriteLine("\nMy Results:");
        foreach (var r in userResults)
            Console.WriteLine($"{r.Date}: {r.QuizTitle} - {r.Score} points");
    }
    static void ShowTopResults()
    {
        Console.WriteLine("\nEnter quiz title for Top-20:");
        string quizName = Console.ReadLine();

        var topResults = results.Where(r => r.QuizTitle.Equals(quizName, StringComparison.OrdinalIgnoreCase))
                                .OrderByDescending(r => r.Score)
                                .ThenBy(r => r.Date)
                                .Take(20)
                                .ToList();

        if (!topResults.Any())
        {
            Console.WriteLine("No results for this quiz.");
            return;
        }

        Console.WriteLine($"\n Top-20: {quizName}");
        int rank = 1;
        foreach (var r in topResults)
        {
            Console.WriteLine($"{rank}. {r.UserLogin} - {r.Score} points ({r.Date})");
            rank++;
        }
    }
    static void EditUserSettings(User user)
    {
        Console.WriteLine("\n Account Settings ");

        Console.Write($"New login (leave empty to skip) [{user.Login}]: ");
        var newLogin = Console.ReadLine();
        if (!string.IsNullOrEmpty(newLogin))
        {
            if (users.Any(u => u.Login == newLogin))
                Console.WriteLine("Login already exists! Not changed.");
            else
            {
                string oldLogin = user.Login;
                user.Login = newLogin;
                foreach (var r in results.Where(r => r.UserLogin == oldLogin))
                    r.UserLogin = newLogin;
                Console.WriteLine("Login updated!");
            }
        }

        Console.Write("New password (leave empty to skip): ");
        var newPass = Console.ReadLine();
        if (!string.IsNullOrEmpty(newPass)) user.Password = newPass;

        Console.Write("New birth date (yyyy-mm-dd, leave empty to skip): ");
        var dateInput = Console.ReadLine();
        if (!string.IsNullOrEmpty(dateInput) && DateTime.TryParse(dateInput, out var newDate))
            user.BirthDate = newDate;

        DataService.Save(usersFile, users);
        DataService.Save(resultsFile, results);

        Console.WriteLine("Settings updated!");
    }
}
