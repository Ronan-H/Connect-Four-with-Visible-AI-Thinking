using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Connect_Four_with_Visible_AI_Thinking
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /* Represents the board state (state of the chips)
         * 0 -> blank (no chip)
         * 1 -> red chip
         * 2 -> yellow chip
         */
        int[,] _board = new int[6, 7];
        Chip[,] _chips = new Chip[6, 7];
        Color[] chipColors = {Colors.White, Colors.Red, Colors.Yellow};

        // 0 = game finished
        // 1 = user's turn
        // 2 = AI's turn
        int _turn = 1;
        int _searchDepth = 4;
        int _bestMove = -1;
        bool _updatingBoard;

        public MainPage()
        {
            this.InitializeComponent();
        }

        bool setupDone = false;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // setup the elements

            // scale the board correctly
            Loaded += delegate
            {
                if (!setupDone)
                {
                    boardGrid.Width = (rootPanel.ActualHeight / 6) * 7;
                    settingsPanel.Width = rootPanel.ActualWidth - boardGrid.Width;

                    int borderSize = (int)(boardGrid.Width / 7) - 2;

                    for (int i = 0; i < 6; ++i)
                    {
                        boardGrid.RowDefinitions.Add(new RowDefinition());
                    }

                    for (int i = 0; i < 7; ++i)
                    {
                        boardGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    }

                    // load the chips
                    for (int i = 0; i < 6; ++i)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            Border border = new Border();
                            border.Width = borderSize;
                            border.Height = borderSize;
                            border.HorizontalAlignment = HorizontalAlignment.Center;
                            border.VerticalAlignment = VerticalAlignment.Center;
                            border.Background = new SolidColorBrush(Colors.Blue);

                            Ellipse chip = new Ellipse();
                            Binding binding = new Binding();
                            _chips[i, j] = new Chip(new SolidColorBrush(chipColors[0]));
                            binding.Source = _chips[i, j];
                            binding.Path = new PropertyPath("ChipChanged");
                            binding.Mode = BindingMode.OneWay;
                            BindingOperations.SetBinding(chip, Ellipse.FillProperty, binding);
                            chip.Width = (int)(borderSize * 0.8);
                            chip.Height = chip.Width;

                            _board[i, j] = 0;

                            border.SetValue(Grid.RowProperty, i);
                            border.SetValue(Grid.ColumnProperty, j);

                            chip.SetValue(Grid.RowProperty, i);
                            chip.SetValue(Grid.ColumnProperty, j);

                            boardGrid.Children.Add(border);
                            boardGrid.Children.Add(chip);
                        }
                    }

                    setupDone = true;
                }
            };
        }

        private async void OnBoardTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_turn == 1)
            {
                int column = (int)(e.GetPosition((UIElement)sender).X / (boardGrid.Width / 7));

                if (!isColumnFull(column))
                {
                    placeChip(1, column);
                    await updateBoard();
                    _turn = 2;
                    await Task.Run(() => doAiMove());
                    await updateBoard();
                }
            }
        }

        private bool isColumnFull(int column)
        {
            return _board[0, column] != 0;
        }

        private bool isBoardFull()
        {
            for (int i = 0; i < 7; ++i)
            {
                if (!isColumnFull(i)) return false;
            }

            return true;
        }

        private void placeChip(int player, int column)
        {
            for (int i = 5; i >= 0; --i)
            {
                if (_board[i, column] == 0)
                {
                    _board[i, column] = player;
                    return;
                }
            }
        }


        // Remove the top most chip from a column.;
        private void removeChip(int column)
        {
            for (int i = 0; i < 6; ++i)
            {
                if (_board[i, column] != 0)
                {
                    _board[i, column] = 0;
                    return;
                }
            }
        }
        
        

        public async Task updateBoard()
        {
            Debug.WriteLine("Start of update");
            for (int i = 0; i < 6; ++i)
            {
                for (int j = 0; j < 7; ++j)
                {
                    Color chipColor = chipColors[_board[i, j]];

                    try
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            if ((_chips[i, j]._color).Color.Equals(chipColor) == false)
                            {
                                _chips[i, j].ChipChanged = new SolidColorBrush(chipColor);
                            }
                        });
                    }
                    catch (Exception e) { }
                    
                }
            }

            Debug.WriteLine("End of update");
            _updatingBoard = false;
        }

        private async Task doAiMove()
        {
            minMax(true, _searchDepth, true);
            placeChip(2, _bestMove);
            _turn = 1;
        }

        /* Finds how good the board is for the AI.
         * This is how terminal MinMax nodes are evaluated.
         * A value of >0 indicates the AI is winning.
         * A value of <0 indicates the player is winning.
         * A value of 0 indicates a tied board state.
         */
        private int getBoardEvaluation()
        {
            return getPlayerChipsValue(2) - getPlayerChipsValue(1);
        }

        private int minMax(bool topLevel, int depth, bool maximizingPlayer)
        {
            if (depth == 0 || isBoardFull())
            {
                return getBoardEvaluation();
            }
            else
            {
                /* This is laid out so that the boards aren't
                 * evaluated twice if the game is won.
                 * 
                 * (i.e. checking isGameWon() then returning getBoardEvaluation()
                 * is very inefficient; the game state is evaluated twice)
                 */

                int playerValue = getPlayerChipsValue(1);
                int aiValue = getPlayerChipsValue(2);

                if (Math.Abs(playerValue) == Int32.MaxValue || Math.Abs(aiValue) == Int32.MaxValue)
                {
                    return aiValue - playerValue;
                }
            }

            if (maximizingPlayer)
            {
                int bestValue = Int32.MinValue;

                for (int i = 0; i < 7; ++i)
                {
                    if (!isColumnFull(i)) {
                        placeChip(2, i);
                        Task.Run(() => updateBoard()).Wait();
                        Task.Delay(10).Wait();
                        Debug.WriteLine("After run");
                        int value = minMax(false, depth - 1, false);
                        if (topLevel) Debug.WriteLine("Col: " + i + " Value: " + value);
                        if (value >= bestValue)
                        {
                            bestValue = value;
                            if (topLevel)
                            {
                                _bestMove = i;
                            }
                        }
                        removeChip(i);
                    }
                }

                return bestValue;
            }
            else
            {
                // Minimizing player
                int bestValue = Int32.MaxValue;
                
                for (int i = 0; i < 7; ++i)
                {
                    if (!isColumnFull(i))
                    {
                        placeChip(1, i);
                        int value = minMax(false, depth - 1, true);
                        bestValue = Math.Min(bestValue, value);
                        removeChip(i);
                    }
                }

                return bestValue;
            }
        }

        /* The offsets used when checking for potential 4-in-a-rows.
             * 
             * In this order:
             * Going...
             * - up, left
             * - up
             * - up, right
             * - right
             */
        int[,] _offsets = new int[,]
        {
                {-1, -1},
                {0, -1},
                {1, -1},
                {1, 0}
        };
        private int getPlayerChipsValue(int player)
        {
            /* Evaluation method:
             * 
             * -> 0 points for 0 in a row
             * -> 1 point for 1 in a row
             * -> 4 points for 2 in a row
             * -> 9 points for 3 in a row
             * -> infinte points for 4 in a row
             * 
             * Only chips with potential to be part of a four-in-a-row
             * will add value.
             * 
             * Chips with potential to be part of a four-in-a-row in multiple
             * directions will be counted multiple times.
             * 
             * Potential future improvement: less value given to potential four-in-a-rows
             * who need chips below them first (since they are harder to build).
             */

            int value = 0;
            
            int[] rewards = {0, 1, 4, 9, Int32.MaxValue};
            int otherPlayer = (player == 1 ? 2 : 1);

            for (int y = 0; y < 6; ++y)
            {
                for (int x = 0; x < 7; ++x)
                {
                    // Check each direction for a potential 4-in-a-row
                    for (int off = 0; off < _offsets.GetLength(0); ++off)
                    {
                        if (!inBounds(x + _offsets[off, 0] * 3, y + _offsets[off, 1] * 3))
                        {
                            continue;
                        }

                        /* The number of chips the player owns in this potential
                         * 4-in-a-row.
                         */
                        int playerChips = 0;
                        for (int offLen = 0; offLen < 4; ++offLen)
                        {
                            int currentChip = _board[y + _offsets[off, 1] * offLen, x + _offsets[off, 0] * offLen];

                            if (currentChip == otherPlayer)
                            {
                                /* The other player has a chip, so this player
                                 * cannot get a 4-in-a-row here.
                                 */
                                goto endOffsetLoop;
                            }

                            if (currentChip == player)
                            {
                                ++playerChips;
                            }
                        }
                        
                        if (playerChips == 4)
                        {
                            return rewards[4];
                        }
                        else
                        {
                            value += rewards[playerChips];
                        }

                        endOffsetLoop:;
                    }
                }
            }
            
            return value;
        }

        private bool inBounds(int x, int y)
        {
            return x >= 0 && x < 7
                && y >= 0 && y < 6;
        }

        public bool isGameWon()
        {
            return getPlayerChipsValue(2) == Int32.MaxValue
                || getPlayerChipsValue(1) == Int32.MaxValue;
        }

    }
}
