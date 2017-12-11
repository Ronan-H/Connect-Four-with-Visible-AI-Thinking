using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
        Ellipse[,] _chips = new Ellipse[6, 7];
        Color[] chipColors = {Colors.White, Colors.Red, Colors.Yellow};

        public MainPage()
        {
            this.InitializeComponent();
        }

        Boolean setupDone = false;
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
                            chip.Fill = new SolidColorBrush(chipColors[0]);
                            chip.Width = (int)(borderSize * 0.8);
                            chip.Height = chip.Width;

                            _chips[i, j] = chip;
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

        private void OnBoardTapped(object sender, TappedRoutedEventArgs e)
        {
            int column = (int) (e.GetPosition((UIElement) sender).X / (boardGrid.Width / 7));

            if (!isColumnFull(column))
            {
                placeChip(1, column);
                updateBoard();
            }
        }

        private Boolean isColumnFull(int column)
        {
            return _board[0, column] != 0;
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

        public void updateBoard()
        {
            for (int i = 0; i < 6; ++i)
            {
                for (int j = 0; j < 7; ++j)
                {
                    Color chipColor = chipColors[_board[i, j]];
                    if ((_chips[i, j].Fill as SolidColorBrush).Color.Equals(chipColor) == false)
                    {
                        _chips[i, j].Fill = new SolidColorBrush(chipColor);
                    }
                }
            }
        }

    }
}
