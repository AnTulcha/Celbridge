import sys
from textblob import Word

if __name__ == '__main__':
    word = ""
    if len(sys.argv) == 1:
        print("")
    else:
        arg = sys.argv[1]
        word = Word(arg)
        result = word.correct()
        print(result)

