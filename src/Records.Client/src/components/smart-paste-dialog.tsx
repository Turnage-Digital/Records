import * as React from "react";

import { ContentPaste } from "@mui/icons-material";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
} from "@mui/material";

interface Props {
  open: boolean;
  onClose: () => void;
  onPaste: (text: string) => void;
}

const SmartPasteDialog = ({ open, onClose, onPaste }: Props) => {
  const [text, setText] = React.useState("");

  React.useEffect(() => {
    if (!open) {
      setText("");
    }
  }, [open]);

  const handleClose = () => {
    onClose();
  };

  const handlePasteClick = () => {
    onPaste(text);
    onClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>Smart Paste</DialogTitle>
      <DialogContent>
        <TextField
          multiline
          fullWidth
          minRows={8}
          placeholder="Paste your text here..."
          value={text}
          onChange={(e) => setText(e.target.value)}
          inputRef={(node) => {
            if (open && node) {
              node.focus();
            }
          }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button
          variant="contained"
          startIcon={<ContentPaste />}
          disabled={text.trim().length === 0}
          onClick={handlePasteClick}
        >
          Smart Paste
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default SmartPasteDialog;
