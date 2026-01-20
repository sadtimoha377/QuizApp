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
            Console.WriteLine("\n$$$ Quiz App $$$");
            Console.WriteLine("1 - Log In");
            Console.WriteLine("2 - Sign Up");
            Console.WriteLine("0 - Exit");
            Console.Write("Your choice: ");

            var choice = Console.ReadLine();
            if (choice == "0") break;

            if (choice == "2") Register();
            else if (choice == "1") Login();
            else Console.WriteLine("Invalid, option!");
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

        Console.Write("Password (min 4 chars): ");
        var password = Console.ReadLine();
        if (password.Length < 4)
        {
            Console.WriteLine("Password too short!");
            return;
        }

        DateTime birthDate;
        while (true)
        {
            Console.Write("Birth date y-m-d(full): ");
            if (DateTime.TryParse(Console.ReadLine(), out birthDate)) break;
            Console.WriteLine("Invalid, date!");
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

        static void UserMenu(User user)
        {
            while (true)
            {
                Console.WriteLine("\n$$$ Menu $$$");
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
                    default: Console.WriteLine("Invalid, option!"); break;
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
                Console.WriteLine("Invalid, Enter 1 2 or 3.");

            string filePath = difficulty switch
            {
                1 => "Data/easy.txt",
                2 => "Data/medium.txt",
                3 => "Data/hard.txt",
                _ => "Data/easy.txt"
            };

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File {filePath} not found!");
                return;
            }

            var allQuestions = LoadQuestions(filePath);
            var random = new Random();
            var selected = allQuestions.OrderBy(q => random.Next()).Take(20).ToList();

            int score = 0;
            int qNum = 1;

            foreach (var q in selected)
            {
                Console.WriteLine($"\nQuestion {qNum}/{selected.Count}: {q.Text}");
                for (int i = 0; i < q.Answers.Count; i++)
                    Console.WriteLine($"{i + 1}. {q.Answers[i].Text}");

                List<int> input;
                while (true)
                {
                    Console.Write($"Your choice 1to{q.Answers.Count}): ");
                    string answer = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(answer))
                    {
                        Console.WriteLine("Enter something!!!!!");
                        continue;
                    }

                    try
                    {
                        input = answer.Split(',')
                            .Select(x => int.Parse(x.Trim()) - 1)
                            .Where(x => x >= 0 && x < q.Answers.Count)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

                        if (input.Count > 0) break;
                        else Console.WriteLine("Invalid numbers!");
                    }
                    catch
                    {
                        Console.WriteLine("Use numbers like '1' or '1,3'");
                    }
                }

                var correct = q.Answers.Select((a, i) => a.IsCorrect ? i : -1)
                                       .Where(i => i != -1)
                                       .OrderBy(i => i)
                                       .ToList();

                if (input.SequenceEqual(correct)) score++;
                qNum++;
            }

            results.Add(new QuizResult
            {
                UserLogin = user.Login,
                QuizTitle = $"Level {difficulty}",
                Score = score,
                Date = DateTime.Now
            });
            DataService.Save(resultsFile, results);

            Console.WriteLine($"\nResult: {score}/{selected.Count} points!");
            Console.WriteLine($"Percentage: {(score * 100.0 / selected.Count):F1}%");
            Console.ReadKey();
        }

        static List<Question> LoadQuestions(string path)
        {
            var questions = new List<Question>();
            var lines = File.ReadAllLines(path);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(' ');
                if (parts.Length < 5) continue;

                var q = new Question();
                q.Text = string.Join(" ", parts.Take(parts.Length - 4));

                var answers = parts.Skip(parts.Length - 4).ToList();
                foreach (var a in answers)
                {
                    bool correct = a.EndsWith("*");
                    string text = correct ? a.TrimEnd('*') : a;
                    q.Answers.Add(new Answer { Text = text, IsCorrect = correct });
                }

                var rnd = new Random();
                q.Answers = q.Answers.OrderBy(x => rnd.Next()).ToList();
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
                Console.WriteLine("\nNo results yet.");
                return;
            }

            Console.WriteLine("\nMy Results:");
            foreach (var r in userResults)
                Console.WriteLine($"{r.Date:MM/dd HH:mm} - {r.QuizTitle}: {r.Score} pts");
        }

        static void ShowTopResults()
        {
            Console.Write("\nEnter quiz title: ");
            string quizName = Console.ReadLine();

            var topResults = results.Where(r => r.QuizTitle.Contains(quizName))
                                    .OrderByDescending(r => r.Score)
                                    .ThenBy(r => r.Date)
                                    .Take(20)
                                    .ToList();

            if (!topResults.Any())
            {
                Console.WriteLine("No results for this quiz.");
                return;
            }

            Console.WriteLine($"\nTop-20: {quizName}");
            for (int i = 0; i < topResults.Count; i++)
                Console.WriteLine($"{i + 1}. {topResults[i].UserLogin} - {topResults[i].Score} pts");
        }

        static void EditUserSettings(User user)
        {
            Console.WriteLine("\nAccount Settings");

            Console.Write($"New login [{user.Login}]: ");
            var newLogin = Console.ReadLine();
            if (!string.IsNullOrEmpty(newLogin))
            {
                if (users.Any(u => u.Login == newLogin))
                    Console.WriteLine("Login exists! Not changed.");
                else
                {
                    foreach (var r in results.Where(r => r.UserLogin == user.Login))
                        r.UserLogin = newLogin;

                    user.Login = newLogin;
                    Console.WriteLine("Login updated!");
                }
            }

            Console.Write("New password: ");
            var newPass = Console.ReadLine();
            if (!string.IsNullOrEmpty(newPass))
            {
                if (newPass.Length < 4)
                    Console.WriteLine("Password too short! Not changed.");
                else
                {
                    user.Password = newPass;
                    Console.WriteLine("Password updated!");
                }
            }

            Console.Write("New birth date y-m-d(full): ");
            var dateInput = Console.ReadLine();
            if (!string.IsNullOrEmpty(dateInput) && DateTime.TryParse(dateInput, out var newDate))
                user.BirthDate = newDate;

            DataService.Save(usersFile, users);
            DataService.Save(resultsFile, results);
            Console.WriteLine("Settings saved!");
        }
    }
}
