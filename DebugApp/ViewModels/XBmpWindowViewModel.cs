using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DebugApp
{
    public class XBmpWindowViewModelDummy : XBmpWindowViewModel
    {
        public XBmpWindowViewModelDummy()
        {
            Width = 3;
            Height = 4;
            ClearField();
            Bitmap[0][0].Value = true;
            Bitmap[2][3].Value = true;
        }
    }

    public class XBmpWindowViewModel : BindableBase
    {
        public ObservableCollection<ObservableCollection<Cell>> Bitmap { get; } = new ObservableCollection<ObservableCollection<Cell>>();
        private int _width = 3;
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }
        private int _height = 12;
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }
        private string _importText;
        public string ImportText
        {
            get => _importText;
            set => SetProperty(ref _importText, value);
        }
        private string _exportText;
        public string ExportText
        {
            get => _exportText;
            set => SetProperty(ref _exportText, value);
        }

        public DelegateCommand ClearFieldCommand { get; }
        public DelegateCommand InvertFieldCommand { get; }
        public DelegateCommand ImportCommand { get; }
        public DelegateCommand ExportCommand { get; }

        public XBmpWindowViewModel()
        {
            ClearFieldCommand = new DelegateCommand(ClearField);
            InvertFieldCommand = new DelegateCommand(InvertField);
            ImportCommand = new DelegateCommand(Import);
            ExportCommand = new DelegateCommand(Export);
            ClearField();
        }

        protected void ClearField()
        {
            Bitmap.Clear();
            for (int x = 0; x < Width; x++)
            {
                var col = new ObservableCollection<Cell>();
                for (int y = 0; y < Height; y++)
                    col.Add(new Cell(false));
                Bitmap.Add(col);
            }
        }
        protected void InvertField()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Bitmap[x][y].Value = !Bitmap[x][y].Value;
        }

        private void Import()
        {
            var hexData = Regex.Replace(ImportText, @"[^\w\d]|(0x)", String.Empty);
            var bytes = Enumerable.Range(0, hexData.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Reverse(Convert.ToByte(hexData.Substring(x, 2), 16)))
                .ToArray();

            int[] arr = new int[100];

            for (int t = 0; t < 100; t++)
                arr[t] = 3 << t;


            var bitArray = ConvertHexToBitArray(bytes);
            //System.IO.File.WriteAllBytes("hexdata.xbmp", BitArrayToByteArray(bitArray));
            int i = 0;
            int rWidth = (Width + 7) & (-8);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < rWidth; x++)
                {
                    //Bitmap[x][y].Value = bitArray[i];
                    if (x < Width)
                        if ((((bytes[i / 8] >> (i % 8)) & 1) == 1))
                            Bitmap[x][y].Value = true;
                        else
                            Bitmap[x][y].Value = false;
                    i++;
                }
        }

        public static byte Reverse(byte b)
        {
            return b;
            byte r = 0;
            for (byte i = 0; i < 8; i++)
                r = (byte)((r << 1) + ((b >> i) & 1));
            return r;
        }

        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }
        public static BitArray ConvertHexToBitArray(byte[] bytes)
        {
            BitArray ba = new BitArray(8 * bytes.Length);
            for (int i = 0; i < bytes.Length; i++)
                for (int j = 0; j < 8; j++)
                    ba[i * 8 + j] = ((bytes[i] >> j) & 1) == 1;
            return ba;
        }
        public static BitArray ConvertHexToBitArray(string hexData)
        {
            if (hexData == null)
                return null; // or do something else, throw, ...

            BitArray ba = new BitArray(4 * hexData.Length);
            for (int i = 0; i < hexData.Length; i++)
            {
                byte b = byte.Parse(hexData[i].ToString(), NumberStyles.HexNumber);
                for (int j = 0; j < 4; j++)
                {
                    ba.Set(i * 4 + j, (b & (1 << (3 - j))) != 0);
                }
            }
            return ba;
        }

        private void Export()
        {

        }
    }
}
