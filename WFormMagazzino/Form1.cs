using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static WFormMagazzino.Form1;

namespace WFormMagazzino
{
    public partial class Form1 : Form
    {
        public List<Indice> indici = new List<Indice>();

        const string FILE_ARCHIVIO = "ARCHIVIO.dat";
        const string FILE_INDICI = "INDICI.txt";

        const int DIM_NOME = 15;
        const int DIM_RECORD = 4 + DIM_NOME + 16 + 4 + 1;

        static int ID_COUNTER;

        public Form1()
        {
            InitializeComponent();
            GeneraFile();

            AggiornaDGV();
        }

        // ** FUNZIONI **
        public void GeneraFile()
        {
            using (FileStream fs = new FileStream(FILE_INDICI, FileMode.OpenOrCreate))
            {
            }

            using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.OpenOrCreate))
            {
            }

            
            foreach (string riga in File.ReadAllLines(FILE_INDICI))
            {
                if (string.IsNullOrWhiteSpace(riga))
                    continue;

                string[] dati = riga.Split(';');

                int codice = int.Parse(dati[0]);
                int posizione = int.Parse(dati[1]);

                Indice i = new Indice(codice, posizione);

                indici.Add(i);
            }

            // ORDINA GLI INDICI PER CODICE CRESCENTE
            indici.Sort((a, b) => a.Codice.CompareTo(b.Codice));

            // ASSEGNA IL CODICE PIÙ ALTO A ID_COUNTER
            if (indici.Count > 0)
            {
                ID_COUNTER = indici[indici.Count - 1].Codice;
            }
            else
            {
                ID_COUNTER = 0;
            }
        }

        public void AggiornaDGV()
        {
            dataGridView1.Rows.Clear();

            foreach (Indice indice in indici)
            {
                Prodotto p = LeggiProdotto(indice.Posizione);

                if (p != null && p.IsActive)
                {
                    dataGridView1.Rows.Add(
                        p.Codice,
                        p.Nome,
                        p.Prezzo,
                        p.Quantita
                    );
                }
            }
        }

        public Prodotto LeggiProdotto(int posizione)
        {
            using(FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.Open))
            {
                using (BinaryReader bw = new BinaryReader(fs))
                {
                    fs.Seek(posizione, SeekOrigin.Begin);
                    return new Prodotto(bw.ReadInt32(), new string(bw.ReadChars(DIM_NOME)).TrimEnd(), bw.ReadDecimal(), bw.ReadInt32(), bw.ReadBoolean());
                }
            }
        }

        public void ScriviProdotto(FileStream fs, Prodotto p, int posizione)
        {
            fs.Seek(posizione, SeekOrigin.Begin);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(p.Codice);

                // NOME LENGTH_FISSA
                string nomeFisso = p.Nome.PadRight(DIM_NOME);
                bw.Write(System.Text.Encoding.ASCII.GetBytes(nomeFisso));

                bw.Write(p.Prezzo);
                bw.Write(p.Quantita);
                bw.Write(p.IsActive);
            }
        }

        public int BinarySearch(List<Indice> indici, int codiceDaCercare)
        {
            int sinistra = 0;
            int destra = indici.Count - 1;

            while (sinistra <= destra)
            {
                int centro = (sinistra + destra) / 2;

                if (indici[centro].Codice == codiceDaCercare)
                {
                    return indici[centro].Posizione;
                }
                else if (indici[centro].Codice < codiceDaCercare)
                {
                    sinistra = centro + 1;
                }
                else
                {
                    destra = centro - 1;
                }
            }

            return -1; // NOT FOUND
        }

        // ** CLASSI **
        public class Prodotto
        {
            private int _codice;

            public int Codice
            {
                get { return _codice; }
            }

            private string _nome;

            public string Nome
            {
                get { return _nome; }
                set { _nome = value; }
            }

            private decimal _prezzo;

            public decimal Prezzo
            {
                get { return _prezzo; }
                set { _prezzo = value; }
            }

            private int _quantita;

            public int Quantita
            {
                get { return _quantita; }
                set { _quantita = value; }
            }

            private bool _isActive;

            public bool IsActive
            {
                get { return _isActive; }
                set { _isActive = value; }
            }


            public Prodotto(int c, string n, decimal p, int q, bool i)
            {
                _codice = c;
                _nome = n;
                _prezzo = p;
                _quantita = q;
                _isActive = i;
            }

            public Prodotto(string n, decimal p, int q)
            {
                _codice = ++ID_COUNTER;
                _nome = n;
                _prezzo = p;
                _quantita = q;
                _isActive = true;
            }

            public Prodotto()
            {
                _codice = 0;
                _nome = string.Empty;
                _prezzo = 0;
                _quantita = 0;
                _isActive = false;
            }
        }

        public class Indice
        {
            private int _codice;

            public int Codice
            {
                get { return _codice; }
                set { _codice = value; }
            }

            private int _posizione;

            public int Posizione
            {
                get { return _posizione; }
                set { _posizione = value; }
            }

            public Indice(int c, int p)
            {
                _codice = c;
                _posizione = p;
            }
        }

        // ** BUTTONS **
        private void btnInserisci_Click(object sender, EventArgs e)
        {
            string nome = txtNome.Text.Trim();

            if (string.IsNullOrWhiteSpace(nome))
            {
                MessageBox.Show("INSERISCI IL NOME.");
                return;
            }

            if (nome.Length > DIM_NOME)
            {
                MessageBox.Show($"IL NOME PUO' CONTENERE MASSIMO {DIM_NOME} CARATTERI.");
                return;
            }

            decimal prezzo = nudPrezzo.Value;
            int quantita = (int) nudQty.Value;

            // CREA PRODOTTO
            Prodotto p = new Prodotto(nome, prezzo, quantita);

            int posizione;
            using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.Open))
            {
                posizione = (int)fs.Length;
                ScriviProdotto(fs, p, posizione);
            }

            // SCRIVI INDICE
            Indice i = new Indice(p.Codice, posizione);
            indici.Add(i);

            // ORDINA
            indici.Sort((a, b) => a.Codice.CompareTo(b.Codice));

            // RISCRIVE IL FILE
            using (StreamWriter sw = new StreamWriter(FILE_INDICI, false))
            {
                foreach (Indice indice in indici)
                {
                    sw.WriteLine($"{indice.Codice};{indice.Posizione}");
                }
            }

            // AGGIORNA DGV
            dataGridView1.Rows.Add(
                p.Codice,
                p.Nome,
                p.Prezzo,
                p.Quantita
            );

            // PULISCI CAMPI
            txtNome.Clear();
            nudPrezzo.Value = nudPrezzo.Minimum;
            nudQty.Value = nudQty.Minimum;

            MessageBox.Show("PRODOTTO INSERITO.");
        }

        private void btnCerca_Click(object sender, EventArgs e)
        {
            try
            {
                int codice = Convert.ToInt32(txtCodice.Text);

                int posizone = BinarySearch(indici, codice);
                if (posizone == -1)
                {
                    MessageBox.Show("CODICE NON PRESENTE.");
                    return;
                }

                Prodotto p = LeggiProdotto(posizone);
                txtCodice.Text = p.Codice.ToString();
                txtNome.Text = p.Nome.ToString();
                nudPrezzo.Value = p.Prezzo;
                nudQty.Value = p.Quantita;
            }
            catch (Exception) 
            {
                MessageBox.Show("CODICE NON VALIDO.");
                return;
            }
        }

        private void btnModifica_Click(object sender, EventArgs e)
        {
            string nome = txtNome.Text.Trim();

            if (string.IsNullOrWhiteSpace(nome))
            {
                MessageBox.Show("INSERISCI IL NOME.");
                return;
            }

            if (nome.Length > DIM_NOME)
            {
                MessageBox.Show($"IL NOME PUO' CONTENERE MASSIMO {DIM_NOME} CARATTERI.");
                return;
            }

            decimal prezzo = nudPrezzo.Value;
            int quantita = (int)nudQty.Value;

            try
            {
                int codice = Convert.ToInt32(txtCodice.Text);
                int posizone = BinarySearch(indici, codice);
                if (posizone == -1)
                {
                    MessageBox.Show("CODICE NON PRESENTE.");
                    return;
                }

                Prodotto p = LeggiProdotto(posizone);
                p.Nome = nome;
                p.Prezzo = prezzo;
                p.Quantita = quantita;

                using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.Open))
                {
                    ScriviProdotto(fs, p, posizone);
                }

                MessageBox.Show("PRDOTTO AGGIRNATO.");
                AggiornaDGV();
            }
            catch (Exception)
            {
                MessageBox.Show("CODICE NON VALIDO.");
                return;
            }
        }

        private void btnElimina_Click(object sender, EventArgs e)
        {
            try 
            {
                int codice = Convert.ToInt32(txtCodice.Text);
                int posizone = BinarySearch(indici, codice);
                if (posizone == -1)
                {
                    MessageBox.Show("CODICE NON PRESENTE.");
                    return;
                }

                Prodotto p = LeggiProdotto(posizone);
                p.IsActive = false;

                using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.Open))
                {
                    ScriviProdotto(fs, p, posizone);
                }

                MessageBox.Show("PRDOTTO ELIMINATO.");
                AggiornaDGV();

            } catch (Exception)
            {
                MessageBox.Show("CODICE NON VALIDO.");
                return;
            }
        }
    }
}
