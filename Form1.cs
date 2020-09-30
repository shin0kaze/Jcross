using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jcross
{
    public partial class Form1 : Form
    {
        #region  definitions
        string rules = "1 - Японский кроссворд представляет собой зашифрованный рисунок, в котором цифры слева от рядов и сверху над колонками показывают, сколько групп черных клеток находится в соответствующей линии и сколько черных клеток содержит каждая группа. Например, три числа - 2, 5, 9 - обозначают, что в этом ряду есть три группы, состоящие: первая - из двух, вторая - из пяти, третья - из девяти слитных черных клеток.\r\n" +
        "2 - Группы разделены, по крайней мере, одной пустой клеткой.\r\n" +
        "3 - Группы не всегда начинаются от края поля, т.е. пустые клетки могут быть и по краям рядов.\r\n" +
        "4 - Так что определите, сколько пустых клеток находится между группами черных клеток (маленький совет: в процессе решения отмечайте не только “найденные” вами черные клетки, но и места расположения пустых клеток - это облегчит выполнение рисунка), и если все сделаете правильно, то получится картинка.\r\n" +
        "5 - Если вы никогда не решали таких задач - изучите пример решения.";
        bool[,] map;
        List<Cell> field;
        List<Label> headers;
        Stack<Point> turns;
        Button btnUndo;
        Button btnReset;
        Counter turnCnt;
        int rows;
        int cols;
        int size;
        int spacing;
        #endregion
        public Form1()
        {
            //параметры для игры
            rows = 6;
            cols = 6;
            size = 30;
            spacing = 10;
            //создаем игровое поле
            field = new List<Cell>();
            headers = new List<Label>();
            turns = new Stack<Point>();
            for (int i = 0; i < rows * cols; i++)
            {
                Point coord = new Point(i % cols, i / rows);
                Cell cell = new Cell(coord, size);
                cell.Location = place(coord);
                this.Controls.Add(cell);
                cell.Click += new EventHandler(doTurn);
                field.Add(cell);
                if (i % rows == rows - 1)
                {
                    Label label = new Label();
                    Point location = place(coord);
                    location.X += size + spacing;
                    label.Location = location;
                    label.Size = new Size(size * 3, size);
                    label.BackColor = Color.LightGray;
                    label.TextAlign = ContentAlignment.MiddleCenter;
                    this.Controls.Add(label);
                    headers.Add(label);
                }
            }
            for (int i = 0; i < cols; i++)
            {
                Point coord = new Point(i, rows);
                Label label = new Label();
                label.Location = place(coord);
                label.Size = new Size(size, size * 3);
                label.BackColor = Color.LightGray;
                label.TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(label);
                headers.Add(label);
            }
            //прочие UI элементы
            btnUndo = new Button();
            btnUndo.Text = "Отменить";
            btnUndo.Location = place(new Point(cols, rows));
            btnUndo.Size = new Size((int)(size * 2.5), size);
            this.Controls.Add(btnUndo);
            btnUndo.Click += new EventHandler(backTurn);
            btnReset = new Button();
            btnReset.Text = "Рестарт";
            btnReset.Location = place(new Point(cols, rows + 1));
            btnReset.Size = new Size((int)(size * 2.5), size);
            this.Controls.Add(btnReset);
            btnReset.Click += new EventHandler(reset);
            turnCnt = new Counter(turns);
            turnCnt.Location = place(new Point(cols / 2, -1));
            turnCnt.Size = new Size(size * 3, size);
            turnCnt.BackColor = Color.LightGray;
            turnCnt.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(turnCnt);
            TextBox rulesBox = new TextBox();
            rulesBox.Size = new Size(place(new Point(cols + 1, rows + 2)));
            rulesBox.ReadOnly = true;
            rulesBox.Location = place(new Point(cols + 3, -1));
            rulesBox.Multiline = true;
            rulesBox.Text = rules;
            this.Controls.Add(rulesBox);
            setup();
            Point formSize = place(new Point(cols + 11, rows + 3));
            InitializeComponent(formSize);
        }
        ///<summary>
        ///просто все обнуляем
        ///</summary>
        public void setup()
        {
            field.ForEach(delegate (Cell cell) { cell.reset(); });
            map = read();
            Point coord = new Point(0, -1);
            int i = 0;
            for (; coord.X < rows; i++)
            {
                headers[i].Text = getIslands(coord);
                coord.X++;
            }
            coord = new Point(-1, 0);
            for (; coord.Y < cols; i++)
            {
                headers[i].Text = getIslands(coord);
                coord.Y++;
            }
        }
        public bool[,] read()
        {
            //мне лень генерировать или читать данные откуда либо 
            return new bool[,] {
                {true,true,false,false,true,false},     // X X O O X O
                {true,false,false,false,false,false},   // X O O O O O
                {false,false,false,false,false,false},  // O O O O O O
                {false,true,false,false,false,false},   // O X O O O O
                {false,true,true,false,false,false},    // O X X O O O
                {false,false,false,false,false,false},  // O O O O O O
            };
        }
        ///<summary>
        ///вычисляем расположение
        ///</summary>
        public Point place(Point coord)
        {
            return new Point(-15 + ((coord.X + 1) * (size + spacing)), 15 + ((coord.Y + 1) * (size + spacing)));
        }
        ///<summary>
        ///получаем список островов для Label
        ///</summary>
        private string getIslands(Point coord)
        {
            //считать по горизонтали или вертикали?
            bool hor = coord.X == -1;
            char sep = hor ? '\n' : ' ';
            int length = hor ? cols : rows;
            int x = hor ? 0 : coord.X;
            int y = hor ? coord.Y : 0;
            //ибо строки так то Intern
            StringBuilder sb = new StringBuilder();
            sb.Append(' ');
            int islands = 0;
            for (int i = 0; i < length; i++)
            {
                bool point = map[x, y];
                if (point)
                {
                    islands++;
                }
                else if (islands > 0)
                {
                    sb.Append(islands).Append(sep);
                    islands = 0;
                }
                if (hor) x++; else y++;
            }
            return sb.ToString();
        }
        ///<summary>
        ///ход игрока
        ///</summary>
        private void doTurn(object sender, EventArgs e)
        {
            Cell cell = (sender as Cell);
            if (cell.Taped) return;
            bool turnAllow = turn(cell.Place);
            if (!turnAllow)
            {
                MessageBox.Show("Bad turn!");
                return;
            }
            turnCnt.push(cell.Place);
            bool isWin = checkWin();
            if (isWin)
            {
                MessageBox.Show("You win!");
            }
        }
        ///<summary>
        ///достаем прошлые ходы из стека
        ///</summary>
        private void backTurn(object sender, EventArgs e)
        {
            if (turns.Count < 1)
            {
                MessageBox.Show("No aviable turns!");
                return;
            }
            turn(turnCnt.pop());
        }
        ///<summary>
        ///пробуем закрасить клетку
        ///</summary>
        private bool turn(Point coord)
        {
            if (map[coord.Y, coord.X])
            {
                field[coord.Y * cols + coord.X].tape();
                return true;
            }
            else return false;
        }
        ///<summary>
        ///просто обнуляем
        ///</summary>
        private void reset(object sender, EventArgs e)
        {
            turnCnt.clear();
            setup();
        }
        ///<summary>
        ///не, ну а вдруг?
        ///п.с.никогда не умел играть в это...
        ///</summary>
        private bool checkWin()
        {
            IEnumerator<Cell> scanner = field.GetEnumerator();
            foreach (bool island in map)
            {
                scanner.MoveNext();
                if (island != scanner.Current.Taped) return false;
            }
            return true;
        }
    }
    ///<summary>
    ///кнопка с возможностью передать свои координаты
    ///</summary>
    class Cell : Button
    {
        public Cell(Point coord, int size)
        {
            Place = coord;
            this.Size = new Size(size, size);
            this.Text = "";
            this.BackColor = Color.White;
        }
        ///<summary>
        ///меняем цвет если нажали
        ///</summary>
        public void tape()
        {
            if (!Taped)
            {
                this.BackColor = Color.Black;
                Taped = true;
            }
            else
            {
                this.BackColor = Color.White;
                Taped = false;
            }
        }
        ///<summary>
        ///обнуляем
        ///</summary>
        public void reset()
        {
            this.BackColor = Color.White;
            Taped = false;
        }
        public bool Taped { get; private set; }
        public Point Place { get; }
    }
    ///<summary>
    ///Вообще не стоит передавать логику в Label, (хотя пофиг)
    ///</summary>
    class Counter : Label
    {
        private Stack<Point> turns;
        public Counter(Stack<Point> turns)
        {
            this.turns = turns;
            this.Text = "Кол-во Ходов: " + turns.Count;
        }
        public void push(Point coord)
        {
            turns.Push(coord);
            this.Text = "Кол-во ходов: " + turns.Count;
        }
        public Point pop()
        {
            this.Text = "Кол-во ходов: " + (turns.Count - 1);
            return turns.Pop();
        }
        public void clear()
        {
            turns.Clear();
            this.Text = "Кол-во Ходов: " + turns.Count;
        }
    }
}
