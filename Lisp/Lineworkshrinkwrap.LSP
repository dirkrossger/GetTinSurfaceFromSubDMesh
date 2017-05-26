(progn
  (setq e (car (entsel "\nSelect ShrinkObject!")))
  (command-s "-lineworkshrinkwrap" e))