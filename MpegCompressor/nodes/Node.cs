﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MpegCompressor {
    //TODO: have node not extend panel, and be drawn purely by NodeView.
    public abstract class Node : IViewable, IProperties {
        public class Address {
            public Node node;
            public string port;
            public Address(Node n, string s) {
                node = n;
                port = s;
            }
            public bool Equals(Address other) {
                return node == other.node && port.Equals(other.port);
            }
        }
        
        private bool isDirty;
        private string extra;
        protected Dictionary<string, Property> properties;
        protected DataBlob state;
        public Point pos;

        public event EventHandler eViewChanged;

        public Node() {
            properties = new Dictionary<string, Property>();
            state = new DataBlob();
            
            Property p = new Property(false, false);
            p.createString("default", "Name of the control");
            p.eValueChanged += (s, e) => fireOutputChanged(e);
            properties.Add("name", p);
            createProperties();
        }

        public void rename(string newName) {
            properties["name"].setString(newName);
        }

        public void setExtra(string sExtra) {
            extra = sExtra;
        }

        public string getName() {
            return properties["name"].getString();
        }

        public string getExtra() {
            return extra;
        }

        protected virtual void createProperties() { }

        public static bool connect(Node from, string fromPort, Node to, string toPort) {
            if (!from.addOutput(fromPort, to, toPort))
                return false;
            if (!to.addInput(toPort, from, fromPort)) {
                from.removeOutput(fromPort, to, toPort);
                return false;
            }
            return true;
        }

        public static void disconnect(Node from, string fromPort, Node to, string toPort) {
            from.removeOutput(fromPort, to, toPort);
            to.removeInput(toPort);
        }

        protected virtual bool addInput(string port, Node from, string fromPort) {
            //if the port is valid
            if (properties.ContainsKey(port)) {
                //if there's an old connection, disconnect both ends
                if (properties[port].input != null) {
                    properties[port].input.node.removeOutput(properties[port].input.port, this, port);
                    properties[port].input = null;
                }
                //place the new connection
                properties[port].input = new Address(from, fromPort);
                soil();
                return true;
            }
            //else fail
            return false;
        }

        protected virtual bool addOutput(string port, Node to, string toPort) {
            //if there's an old connection, doesn't matter. Output can be 1..*
            HashSet<Address> cnx;
            if (properties.ContainsKey(port)) {
                cnx = properties[port].output;
                cnx.Add(new Address(to, toPort));
                return true;
            }
            return false;
        }

        protected virtual void removeInput(string port) {
            //Note: only breaks this end of the connection.
            if (properties.ContainsKey(port)) {
                properties[port].input = null;
            }
            soil();
        }

        protected virtual void removeOutput(string port, Node to, string toPort) {
            //Note: only breaks this end of the connection.
            Address match = new Address(to, toPort);
            if (properties.ContainsKey(port)) {
                //TODO: test this. It uses .Equals() to find the match right?
                properties[port].output.Remove(match);  //returns false if item not found.
            }
        }

        public virtual DataBlob getData(string port) {
            if (isDirty) {
                clean();
            }
            return state;
        }

        public Dictionary<string, Property> getProperties() {
            return properties;
        }

        protected void soil() {
            isDirty = true;
            foreach (KeyValuePair<string, Property> kvp in properties) {
                if (!kvp.Value.isOutput) {
                    return;
                }
                foreach (Address a in kvp.Value.output) {
                    a.node.soil();
                }
            }
            fireOutputChanged(new EventArgs());
        }

        protected virtual void clean() {
            isDirty = false;
        }
        
        private void fireOutputChanged(EventArgs e) {
            EventHandler handler = eViewChanged;
            if (handler != null) {
                handler(this, e);
            }
        }
        
        public virtual Bitmap view() {
            if (isDirty) {
                clean();
            }
            if (state != null) {
                return state.bmp;
            }
            //Debug.Write("View missing in " + properties["name"].getString() + "\n");
            return null;
        }


        public void drawExtra(Graphics g) {
            if (state == null || state.bmp == null) {
                return;
            }
            //draw corner crosses.
            //bottom left
            g.DrawLine(Pens.BlanchedAlmond, -0.5f, -10, -0.5f, 10);
            g.DrawLine(Pens.BlanchedAlmond, -10, -0.5f, 10, -0.5f);

            //bottom right
            g.DrawLine(Pens.BlanchedAlmond, state.bmp.Width + 0.5f, -10, state.bmp.Width + 0.5f, 10);
            g.DrawLine(Pens.BlanchedAlmond, state.bmp.Width - 10, -0.5f, state.bmp.Width + 10, -0.5f);

            //top right
            g.DrawLine(Pens.BlanchedAlmond, state.bmp.Width + 0.5f, state.bmp.Height - 10, state.bmp.Width + 0.5f, state.bmp.Height + 10);
            g.DrawLine(Pens.BlanchedAlmond, state.bmp.Width - 10, state.bmp.Height + 0.5f, state.bmp.Width + 10, state.bmp.Height + 0.5f);

            //top left
            g.DrawLine(Pens.BlanchedAlmond, -0.5f, state.bmp.Height - 10, -0.5f, state.bmp.Height + 10);
            g.DrawLine(Pens.BlanchedAlmond, -10, state.bmp.Height + 0.5f, +10, state.bmp.Height + 0.5f);
        }
    }
}
