﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JigsawPuzzleSolver.GUI_Elements
{
    /// <summary>
    /// Interaction logic for PieceViewer.xaml
    /// </summary>
    public partial class PieceViewer : UserControl
    {
        public static readonly DependencyProperty PieceListDependencyProperty = DependencyProperty.Register("PieceList", typeof(ObservableCollection<Piece>), typeof(PieceViewer));
        public ObservableCollection<Piece> PieceList
        {
            get { return (ObservableCollection<Piece>)GetValue(PieceListDependencyProperty); }
            set { SetValue(PieceListDependencyProperty, value); }
        }

        public PieceViewer()
        {
            InitializeComponent();
        }

    }
}
