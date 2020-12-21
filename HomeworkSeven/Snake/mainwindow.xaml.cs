using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Timers;

/*
                                   Вариант 1
  Добавлено «Супер яблоко». Каждое пятое яблоко с вероятностью 0.5 может
  превратиться в «супер яблоко». Супер яблоко имеет другой спрайт, дает 
  10000 очков и исчезает через 10 секунд, если игрок его не подобрал.
  после этого (предотвращает появление бонусов до начала движения змеи).
 */

namespace Snake
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Field // поле и положение объектов в нём
        {
            protected int m_width;
            protected int m_height;

            Image m_image;
            public Field(int width, int height, string image) // само поле 
            {
                m_width = width;
                m_height = height;
                m_image = new Image
                {
                    Source = (new ImageSourceConverter()).ConvertFromString(image) as ImageSource,
                    Width = width,
                    Height = height
                };
            }

            public Image image // спрайты
            {
                get
                {
                    return m_image;
                }
                set
                {
                    m_image = value;
                }
            }
        }

        public class PosField : Field // позиционирование на поле
        {
            protected int m_x;
            protected int m_y;
            public PosField(int x, int y, int width, int height, string image)
                : base(width, height, image)
            {
                m_x = x;
                m_y = y;
            }

            public virtual void Move() { } // движения по полю

            public int X
            {
                get
                {
                    return m_x;
                }
                set
                {
                    m_x = value;
                }
            }

            public int Y
            {
                get
                {
                    return m_y;
                }
                set
                {
                    m_y = value;
                }
            }
        }

        public class Fruit : PosField // фрукт, которым питается змея
        {
            List<PosField> m_snake;
            public bool isSuper;
            public Fruit(List<PosField> snake)
                : base(0, 0, 40, 40, "pack://application:,,,/Resources/fruit.png")
            {
                m_snake = snake;
                isSuper = false;
                Move();
            }

            public override void Move()
            {
                Random rand = new Random();
                do
                {
                    X = rand.Next(13) * 40 + 40;
                    Y = rand.Next(13) * 40 + 40;
                    bool overlap = false;
                    foreach (var Part in m_snake)
                    {
                        if (Part.X == X && Part.Y == Y)
                        {
                            overlap = true;
                            break;
                        }
                    }
                    if (!overlap)
                        break;
                } while (true);

            }
        }

        public class Head : PosField // голова змеи обосбленно 
        {
            public enum Direction // клавиши при нажатии на которые змея будет реагировать
            {
                RIGHT, DOWN, LEFT, UP, NONE, W, A , S, D
            };

            Direction m_direction;

            public Direction direction // направление
            {
                set
                {
                    m_direction = value;
                    RotateTransform rotateTransform = new RotateTransform(90 * (int)value);
                    image.RenderTransform = rotateTransform;
                }
            }

            public Head() // голова появляется примерно в центре
                : base(280, 280, 40, 40, "pack://application:,,,/Resources/head.png")
            {
                image.RenderTransformOrigin = new Point(0.5, 0.5);
                m_direction = Direction.NONE;
            }

            public override void Move()// движения змеи
            {
                switch (m_direction)
                {
                    case Direction.DOWN:
                        Y += 40;
                        break;
                    case Direction.UP:
                        Y -= 40;
                        break;
                    case Direction.LEFT:
                        X -= 40;
                        break;
                    case Direction.RIGHT:
                        X += 40;
                        break;
                }
            }
        }

        public class BodyPart : PosField // тело змеи(появляется третьим звеном посередине
        {
            PosField m_next;
            public BodyPart(PosField next)
                : base(next.X, next.Y, 40, 40, "pack://application:,,,/Resources/body.png")
            {
                m_next = next;
            }

            public override void Move()
            {
                X = m_next.X;
                Y = m_next.Y;
            }
        }

        Random rand = new Random(); // для генерации супер фруктов

        Field field;  //Игровое поле

        Head head; // голова змеи
        List<PosField> snake; // вся змея
        double acceleration; //коэффициент ускорения

        Fruit fruit;  // фрукт

        //private static Timer sfTimer; // таймер для супер фрукта
        //sfTimer.Interval = 10000;
        int score; //счёт

        private DispatcherTimer mTimer; //таймер по которому движется змея 
        private DispatcherTimer sfTimer; // таймер для суперфрукта

        int modifier;  //подсчёт очков за съеденные фрукты    
        int FruiteCounter;  //счетчик фруктов
        private bool TimeBegin = false; //включение таймера
       // private bool TimeBeginSF = false; //включение таймера для супер фрукта


        public MainWindow() //  при первоначальном запуске программы подготавливаем всё к игре
        {
            InitializeComponent();

            snake = new List<PosField>(); // готовим змею к игре
            field = new Field(600, 600, "pack://application:,,,/Resources/snake.png"); // игровое поле
            mTimer = new DispatcherTimer  //по этому таймеру движется змея
            {
                Interval = new TimeSpan(0, 0, 0, 0, 300)
            };
            mTimer.Tick += new EventHandler(MoveTimer_Tick);

           sfTimer = new DispatcherTimer  //таймер для суперфрукта
            {
                Interval = new TimeSpan(0, 0, 0, 10)
            };
            sfTimer.Tick += new EventHandler(SfDissapear_Tick);
            sfTimer.Stop();
        }

        private void UpdateField() //обновление игрового поля, чтобы показть движение змеи и др.
        {
            SnakeMoving();
            foreach (var Part in snake) //обновляем положение элементов змеи
            {
                Canvas.SetTop(Part.image, Part.Y);
                Canvas.SetLeft(Part.image, Part.X);
            }

            Canvas.SetTop(fruit.image, fruit.Y);   //обновляем положение рукта
            Canvas.SetLeft(fruit.image, fruit.X);

            lblScore.Content = String.Format("{0}00", score);   //обновляем счёт
        }


        void MoveTimer_Tick(object sender, EventArgs e) //змея движется по таймеру
        {
            foreach (var Part in Enumerable.Reverse(snake)) // змея движется как гусеница - с конца
            {
                Part.Move();
            }

            foreach (var Part in snake.Where(x => x != head)) //для всех частей тела змеи
            {

                if (Part.X == head.X && Part.Y == head.Y) //Если голова змеи коснётся любой другой части тела 
                {
                    mTimer.Stop();
                    tbGameOver.Visibility = Visibility.Visible; // Игра окончена
                    button.IsEnabled = true; // можно начать новую игру
                    return;
                }
            }

            if (head.X < 40 || head.X >= 560 || head.Y < 40 || head.Y >= 560) //Если змея касается краёв поля
            {
                mTimer.Stop();
                tbGameOver.Visibility = Visibility.Visible; // Игра окончена
                button.IsEnabled = true; // можно начать новую игру
                return;
            }


            if (head.X == fruit.X && head.Y == fruit.Y) //Если змея съедает фрукт
            {

                if (fruit.isSuper) //увеличиваем счет до 10000 если это супер фрукт
                {
                    score += 100 * modifier;
                }
                else score += modifier;

                FruiteCounter++; //увеличиваем счетчик фруктов

                acceleration *= 0.98; //увеличиваем  скорость

                fruit.Move();  //двигаем фрукт на новое место

                var part = new BodyPart(snake.Last()); // добавляем новый сегмент к змее
                canvas.Children.Add(part.image);
                snake.Add(part);
                if (fruit.isSuper) // змея успевает съесть супер фрукт
                {
                    fruit.isSuper = false;
                    fruit.image.Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Resources/fruit.png") as ImageSource;
                }
                
                mTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)Math.Round(300 * acceleration, 0)); //меняем интервал тика таймера

                if (FruiteCounter != 0 && FruiteCounter % 5 == 0 && rand.Next(2) == 0) //создаём супер фрукт с вероятностью 0.5 на каждом 5-ом фрукте
                {
                    fruit.isSuper = true; // создаём супер фрукт
                    fruit.image.Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Resources/superfruit.png") as ImageSource;
                    sfTimer.Start(); // запускаем таймер до исчезновения супер фрукта

                }
            }

            UpdateField(); //перерисовываем экран
        }
        void SfDissapear_Tick(object sender, EventArgs e) //змея движется по таймеру
        {
            sfTimer.Stop(); // останавливаем таймер
            fruit.isSuper = false; // супер фрукт исчезает
            fruit.Move();
            fruit.image.Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Resources/fruit.png") as ImageSource;
            UpdateField(); //перерисовываем экран
        }   
        // Обработчик нажатия на кнопку клавиатуры
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key) // Анологичные движения выполняются для двух кнопок одновременно - WASDа и стрелочек как в классическом думе
                // Кстати, при управлении двумя раскладками можно очень быстро менять положение головы. Такая простая игра,
                // а уже есть абъюзы, осталось добавить режим с концовкой и можно закреплять WR по категории Any%
            {
                case Key.W: // движение вверх при нажатии W
                case Key.Up: // движение вверх при нажатии стрелочки вверх
                    if (snake.Count != 0 && (snake.Count == 1 || snake[1].X != head.X || snake[1].Y != head.Y - 40))
                    {
                        head.direction = Head.Direction.UP;
                    }
                    if (TimeBegin)
                    {
                        mTimer.Start();
                        TimeBegin = false;
                    }
                    break;

                case Key.S: // движение вверх при нажатии W
                case Key.Down: // движение вниз при нажатии стрелочки вниз
                    if (snake.Count != 0 && (snake.Count == 1 || snake[1].X != head.X || snake[1].Y != head.Y + 40))
                    {
                        head.direction = Head.Direction.DOWN;
                    }
                    if (TimeBegin)
                    {
                        mTimer.Start();
                        TimeBegin = false;
                    }
                    break;

                case Key.A: // движение влево при нажатии A
                case Key.Left: // движение влево при нажатии стрелочки влево
                    if (snake.Count != 0 && (snake.Count == 1 || snake[1].X != head.X - 40 || snake[1].Y != head.Y))
                    {
                        head.direction = Head.Direction.LEFT;
                    }
                    if (TimeBegin)
                    {
                        mTimer.Start();
                        TimeBegin = false;
                    }
                    break;

                case Key.D: // // движение враво при нажатии D
                case Key.Right: // движение вправо при нажатии стрелочки вправо
                    if (snake.Count != 0 && (snake.Count == 1 || snake[1].X != head.X + 40 || snake[1].Y != head.Y))
                    {
                        head.direction = Head.Direction.RIGHT;
                    }
                    if (TimeBegin)
                    {
                        mTimer.Start();
                        TimeBegin = false;
                    }
                    break;
            }
        }

        // начинаем игру: обнуляем все счётчики  и значения
        private void button_Click(object sender, RoutedEventArgs e)
        {
            score = 0; // обнуляем счет

            FruiteCounter = 0;  // обнуляем счетчик фруктов

            modifier = 1; //сбрасываем подсчёт

            acceleration = 1;   // возвращаем обычную скорость

            snake.Clear(); // убираем змею

            canvas.Children.Clear();  // очищаем холст

            tbGameOver.Visibility = Visibility.Hidden; // скрываем надпись "игра окончена"
            button.IsEnabled = false; // делаем кнопку "начать игру" недоступной, чтобы во время игры игрок её случайно не нажал

            canvas.Children.Add(field.image); // добавляем поле на холст
            fruit = new Fruit(snake); // создаём новый фрукт на поле
            canvas.Children.Add(fruit.image);
            head = new Head();   // создаем голову
            snake.Add(head); // добавляем змее голову
            canvas.Children.Add(head.image);

            TimeBegin = true; //запускаем таймер
            UpdateField(); // обновляем поле

        }

        public void SnakeMoving() // движение частей тела змеи
        {
            if (snake.Count != 1) // хвост змеи
            {
                snake[snake.Count - 1].image.Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Resources/tail.png") as ImageSource;
                RotateTransform rotateTransform = new RotateTransform(0);
                if (snake[snake.Count - 2].X == snake[snake.Count - 1].X &&
                    snake[snake.Count - 2].Y + 40 == snake[snake.Count - 1].Y)
                {
                    rotateTransform = new RotateTransform(90);
                }
                if (snake[snake.Count - 2].X == snake[snake.Count - 1].X &&
                    snake[snake.Count - 2].Y - 40 == snake[snake.Count - 1].Y)
                {
                    rotateTransform = new RotateTransform(270);
                }
                if (snake[snake.Count - 2].Y == snake[snake.Count - 1].Y &&
                    snake[snake.Count - 2].X - 40 == snake[snake.Count - 1].X)
                {
                    rotateTransform = new RotateTransform(180);
                }
                snake[snake.Count - 1].image.RenderTransformOrigin = new Point(0.5, 0.5);
                snake[snake.Count - 1].image.RenderTransform = rotateTransform;
            }


            if (snake.Count != 1)  // тело змеи
            {
                for (int i = 1; i < snake.Count - 1; ++i)
                {
                    // прямая часть
                    RotateTransform rotateTransform = new RotateTransform(0);
                    snake[i].image.Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Resources/body.png") as ImageSource;

                    if (snake[i].X == snake[i + 1].X && snake[i].X == snake[i - 1].X)
                    {
                        rotateTransform = new RotateTransform(0);
                    }
                    else if (snake[i].Y == snake[i + 1].Y && snake[i].Y == snake[i - 1].Y) rotateTransform = new RotateTransform(90);
                    else
                    // изгибы
                    {
                        snake[i].image.Source = (new ImageSourceConverter()).ConvertFromString("pack://application:,,,/Resources/corner.png") as ImageSource;
                        if ((snake[i].X == snake[i + 1].X && snake[i].Y + 40 == snake[i + 1].Y &&
                            snake[i].X - 40 == snake[i - 1].X && snake[i].Y == snake[i - 1].Y) ||
                            (snake[i].X == snake[i - 1].X && snake[i].Y + 40 == snake[i - 1].Y &&
                            snake[i].X - 40 == snake[i + 1].X && snake[i].Y == snake[i + 1].Y))
                        {
                            rotateTransform = new RotateTransform(90);
                        }
                        if ((snake[i].X == snake[i + 1].X && snake[i].Y - 40 == snake[i + 1].Y &&
                            snake[i].Y == snake[i - 1].Y && snake[i].X - 40 == snake[i - 1].X) ||
                            (snake[i].X == snake[i - 1].X && snake[i].Y - 40 == snake[i - 1].Y &&
                            snake[i].Y == snake[i + 1].Y && snake[i].X - 40 == snake[i + 1].X))
                        {
                            rotateTransform = new RotateTransform(180);
                        }
                        if ((snake[i].X == snake[i + 1].X && snake[i].Y - 40 == snake[i + 1].Y &&
                            snake[i].X + 40 == snake[i - 1].X && snake[i].Y == snake[i - 1].Y) ||
                            (snake[i].X == snake[i - 1].X && snake[i].Y - 40 == snake[i - 1].Y &&
                            snake[i].X + 40 == snake[i + 1].X && snake[i].Y == snake[i + 1].Y))
                        {
                            rotateTransform = new RotateTransform(270);
                        }
                    }
                    snake[i].image.RenderTransformOrigin = new Point(0.5, 0.5);
                    snake[i].image.RenderTransform = rotateTransform;
                }
            }
        }
    }
}
