using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private const int ROWS = 10;
        private const int COLS = 10;
        public const int MINE_COUNT = 10;

        // 셀 정보 저장용 2차원 배열
        private CellInfo[,] _board = new CellInfo[ROWS, COLS];

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
        }

        // -----------------------------
        // 1. 보드 초기화 및 지뢰 배치
        // -----------------------------
        private void InitializeBoard()
        {
            BoardGrid.Children.Clear(); // 기존 버튼 초기화

            // 2차원 배열 초기화
            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    _board[r, c] = new CellInfo
                    {
                        Row = r,
                        Col = c,
                        IsMine = false,
                        IsRevealed = false,
                        IsFlagged = false,
                        AdjacentMines = 0
                    };
                }
            }

            // 지뢰 배치
            PlaceMines();

            // 인접 지뢰 수 계산
            CalculateAdjacentMines();

            // 실제 화면에 버튼 생성
            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    Button btn = new Button
                    {
                        Width = 40,
                        Height = 40,
                        Tag = _board[r, c], // 버튼 Tag에 셀 정보를 담음
                        FontSize = 16
                    };

                    // 좌클릭 이벤트
                    btn.Click += Cell_Click;
                    // 우클릭 이벤트 (깃발 표시용)
                    btn.MouseRightButtonUp += Cell_RightClick;

                    BoardGrid.Children.Add(btn);
                }
            }
        }

        // -----------------------------
        // 2. 지뢰 랜덤 배치
        // -----------------------------
        private void PlaceMines()
        {
            Random rand = new Random();
            int minesPlaced = 0;

            while (minesPlaced < MINE_COUNT)
            {
                int r = rand.Next(0, ROWS);
                int c = rand.Next(0, COLS);

                if (!_board[r, c].IsMine)
                {
                    _board[r, c].IsMine = true;
                    minesPlaced++;
                }
            }
        }

        // -----------------------------
        // 3. 인접 지뢰 수 계산
        // -----------------------------
        private void CalculateAdjacentMines()
        {
            // 8방향 오프셋
            int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    if (_board[r, c].IsMine) continue;

                    int mineCount = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int nr = r + dr[i];
                        int nc = c + dc[i];
                        if (IsInBounds(nr, nc) && _board[nr, nc].IsMine)
                        {
                            mineCount++;
                        }
                    }
                    _board[r, c].AdjacentMines = mineCount;
                }
            }
        }

        // -----------------------------
        // 4. 좌클릭 이벤트: 셀 열기
        // -----------------------------
        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            var cell = (CellInfo)btn.Tag;
            if (cell.IsFlagged || cell.IsRevealed) return; // 이미 깃발이 있거나 열린 셀은 무시

            // 지뢰면 게임 오버 처리
            if (cell.IsMine)
            {
                RevealAllMines();
                MessageBox.Show("Game Over!");
                return;
            }

            // 안전한 칸이면 열기
            RevealCell(cell.Row, cell.Col);

            // 승리 체크: 지뢰가 아닌 모든 칸이 열렸으면 승리
            if (CheckWin())
            {
                RevealAllMines();
                MessageBox.Show("You Win!");
            }
        }

        // -----------------------------
        // 5. 우클릭 이벤트: 깃발 표시 토글
        // -----------------------------
        private void Cell_RightClick(object sender, MouseButtonEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            var cell = (CellInfo)btn.Tag;
            if (cell.IsRevealed) return; // 열린 셀에는 깃발 표시 불가

            cell.IsFlagged = !cell.IsFlagged; // 토글
            UpdateButtonContent(btn, cell);
        }

        // -----------------------------
        // 6. 셀 열기 (재귀적으로 주변 셀도 열기)
        // -----------------------------
        private void RevealCell(int r, int c)
        {
            if (!IsInBounds(r, c)) return;
            var cell = _board[r, c];

            if (cell.IsRevealed || cell.IsFlagged) return;

            cell.IsRevealed = true;
            // 버튼 업데이트
            Button btn = GetButtonAt(r, c);
            UpdateButtonContent(btn, cell);

            // 인접 지뢰가 0이면 주변 셀도 자동으로 열기
            if (cell.AdjacentMines == 0)
            {
                int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
                int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };
                for (int i = 0; i < 8; i++)
                {
                    RevealCell(r + dr[i], c + dc[i]);
                }
            }
        }

        // -----------------------------
        // 7. 모든 지뢰 공개
        // -----------------------------
        private void RevealAllMines()
        {
            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    if (_board[r, c].IsMine)
                    {
                        Button btn = GetButtonAt(r, c);
                        _board[r, c].IsRevealed = true;
                        UpdateButtonContent(btn, _board[r, c]);
                    }
                }
            }
        }

        // -----------------------------
        // 8. 승리 판정: 지뢰가 아닌 모든 셀이 열렸는지 체크
        // -----------------------------
        private bool CheckWin()
        {
            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    if (!_board[r, c].IsMine && !_board[r, c].IsRevealed)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // -----------------------------
        // 9. 새 게임 버튼 클릭 이벤트
        // -----------------------------
        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeBoard();
        }

        // -----------------------------
        // 보조 메서드들
        // -----------------------------

        // 지정한 좌표가 보드 범위 내에 있는지 확인
        private bool IsInBounds(int r, int c)
        {
            return (r >= 0 && r < ROWS && c >= 0 && c < COLS);
        }

        // (r, c)에 해당하는 버튼을 찾아 반환
        private Button GetButtonAt(int r, int c)
        {
            int index = r * COLS + c;
            return BoardGrid.Children[index] as Button;
        }

        // 버튼의 내용 및 스타일 업데이트
        private void UpdateButtonContent(Button btn, CellInfo cell)
        {
            if (cell.IsFlagged && !cell.IsRevealed)
            {
                btn.Content = "🚩";
            }
            else if (cell.IsRevealed)
            {
                if (cell.IsMine)
                {
                    btn.Content = "💣";
                    btn.Background = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    btn.Content = (cell.AdjacentMines > 0) ? cell.AdjacentMines.ToString() : "";
                    btn.IsEnabled = false;
                    btn.Background = System.Windows.Media.Brushes.LightGray;
                }
            }
            else
            {
                btn.Content = "";
            }
        }

        public void UpdateBombCount()
        {
            bombCountTextBlock.Text = $"남은 폭탄: {MINE_COUNT}";
        }
    }

    // 각 셀의 정보를 저장할 클래스
    public class CellInfo
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsMine { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsFlagged { get; set; }
        public int AdjacentMines { get; set; }
    }
}
