import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Product } from '../../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {

  constructor(private http: HttpClient) {}

  private readonly localHost: string = 'https://localhost:44347/';

  private readonly get: string = 'get';
  private readonly update: string = 'update';
  private readonly delete: string = 'delete';

  private readonly all: string = 'all';
  private readonly bulk: string = 'bulk';

  private readonly productController: string = 'product';
  private readonly swagger: string = 'swagger/index.html';


  checkServerStatus(): Observable<boolean> {
    return this.http
      .get(this.localHost + this.swagger, { responseType: 'text' })
      .pipe(
        map(() => true),
        catchError(() => of(false))
      );
  }

  //#region Product Endpoints
  getProducts(): Observable<Product[]> {
    const url = `${this.localHost}${this.productController}/${this.get}/${this.all}`;
    return this.http.get<Product[]>(url).pipe(
      catchError(this.handleError) // Error handling
    );
  }

  createProducts(products: Product[]): Observable<boolean[]> {
    const url = `${this.localHost}${this.productController}/${this.get}/${this.bulk}`;
    return this.http.post<boolean[]>(url, products).pipe(
      catchError(this.handleError) // Error handling
    );
  }

  updateProducts(products: Product[]): Observable<boolean[]> {
    const url = `${this.localHost}${this.productController}/${this.update}/${this.bulk}`;
    return this.http.put<boolean[]>(url, products).pipe(
      catchError(this.handleError) // Error handling
    );
  }

  deleteProducts(productIds: number[]): Observable<boolean[]> {
    const url = `${this.localHost}${this.productController}/${this.delete}/${this.bulk}`;
    return this.http.put<boolean[]>(url, productIds).pipe(
      catchError(this.handleError) // Error handling
    );
  }

  //#endregion

  private handleError(error: HttpErrorResponse) {
    // In a real-world app, you may want to send the error to a remote logging infrastructure
    let errorMessage = '';
    if (error.error instanceof ErrorEvent) {
      // A client-side or network error occurred. Handle it accordingly.
      errorMessage = `An error occurred: ${error.error.message}`;
    } else {
      // The backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong.
      errorMessage = `Server returned code: ${error.status}, error message is: ${error.message}`;
    }
    console.error(errorMessage);
    return throwError(
      () => new Error('Something went wrong; please try again later.')
    );
  }
}
